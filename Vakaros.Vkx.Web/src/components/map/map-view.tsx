"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { MapContainer, TileLayer, Polyline, CircleMarker, Marker, Tooltip, useMap, useMapEvents } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { useTheme } from "next-themes";
import { n } from "@/lib/schemas";
import { simplifyTrack, simplifyPositionsWithMaxSpeeds, smoothTrackPositions, zoomToTolerance } from "@/lib/track-utils";
import type { RaceMapProps, TrackMode } from "./race-map";

interface InternalProps extends RaceMapProps {
  openSeaMap: boolean;
  trackMode: TrackMode;
  followMode: boolean;
  onExitFollow: () => void;
  fitTick: number;
}

// Heatmap color from speed (m/s normalized over the track range).
function speedColor(t: number): string {
  // 0=blue, 0.25=cyan, 0.5=neon-green, 0.75=yellow, 1=red
  const stops: [number, [number, number, number]][] = [
    [0, [0, 0, 255]],
    [0.25, [0, 255, 255]],
    [0.5, [0, 255, 0]],
    [0.75, [255, 255, 0]],
    [1, [255, 0, 0]],
  ];
  for (let i = 1; i < stops.length; i++) {
    if (t <= stops[i][0]) {
      const [t0, c0] = stops[i - 1];
      const [t1, c1] = stops[i];
      const f = (t - t0) / (t1 - t0);
      const r = Math.round(c0[0] + (c1[0] - c0[0]) * f);
      const g = Math.round(c0[1] + (c1[1] - c0[1]) * f);
      const b = Math.round(c0[2] + (c1[2] - c0[2]) * f);
      return `rgb(${r},${g},${b})`;
    }
  }
  return "rgb(255,0,0)";
}

function ZoomTracker({ onZoom }: { onZoom: (zoom: number) => void }) {
  useMapEvents({ zoomend: (e) => onZoom(e.target.getZoom()) });
  return null;
}

function AutoInvalidateSize() {
  const map = useMap();
  useEffect(() => {
    const container = map.getContainer();
    const observer = new ResizeObserver(() => map.invalidateSize());
    observer.observe(container);
    return () => observer.disconnect();
  }, [map]);
  return null;
}

function FitBounds({ points, fitTick }: { points: L.LatLngExpression[]; fitTick: number }) {
  const map = useMap();
  const pointsRef = useRef(points);
  pointsRef.current = points;
  useEffect(() => {
    if (pointsRef.current.length === 0) return;
    const b = L.latLngBounds(pointsRef.current);
    map.fitBounds(b.pad(0.05));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fitTick, map]);
  return null;
}

function DragTracker({ onDrag }: { onDrag: () => void }) {
  useMapEvents({ dragstart: () => onDrag() });
  return null;
}

function FollowBoat({ point, followMode }: { point: L.LatLngExpression | null; followMode: boolean }) {
  const map = useMap();
  useEffect(() => {
    if (followMode && point) map.panTo(point);
  }, [point, followMode, map]);
  return null;
}

const arrowDivIcon = (heading: number) =>
  L.divIcon({
    className: "",
    html: `<div style="transform: rotate(${heading}deg); width:20px; height:20px;">
      <svg viewBox="0 0 24 24" width="20" height="20">
        <path d="M12 2 L20 22 L12 18 L4 22 Z" fill="#FF4500" stroke="#fff" stroke-width="1"/>
      </svg></div>`,
    iconSize: [20, 20],
    iconAnchor: [10, 10],
  });

export default function MapView({
  positions, race, legs, startLine, playbackPosition, preRacePositions, windowPositions,
  openSeaMap, trackMode, followMode, onExitFollow, fitTick,
}: InternalProps) {
  const { resolvedTheme } = useTheme();
  const [zoom, setZoom] = useState(14);
  const tileUrl = resolvedTheme === "dark"
    ? "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
    : "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png";

  const tolerance = useMemo(() => zoomToTolerance(zoom), [zoom]);

  const smoothedPositions = useMemo(
    () => smoothTrackPositions(positions ?? []),
    [positions]
  );
  const smoothedPreRacePositions = useMemo(
    () => smoothTrackPositions(preRacePositions ?? []),
    [preRacePositions]
  );

  const points = useMemo(
    () => simplifyTrack(smoothedPositions, tolerance),
    [smoothedPositions, tolerance]
  );
  const preRacePoints = useMemo(
    () => simplifyTrack(smoothedPreRacePositions, tolerance),
    [smoothedPreRacePositions, tolerance]
  );

  // For heatmap mode we need per-segment max speed, so simplify with max-speed tracking.
  const heatmapData = useMemo(
    () => simplifyPositionsWithMaxSpeeds(smoothedPositions, tolerance),
    [smoothedPositions, tolerance]
  );
  const heatmapPositions = heatmapData.positions;
  const heatmapMaxSpeeds = heatmapData.maxSpeeds;
  const heatmapPoints = useMemo(
    () => heatmapPositions.map((p) => [n(p.latitude), n(p.longitude)] as [number, number]),
    [heatmapPositions]
  );

  const speedRange = useMemo(() => {
    if (!heatmapMaxSpeeds.length) return { min: 0, max: 1 };
    let min = Infinity, max = -Infinity;
    for (const v of heatmapMaxSpeeds) {
      if (v < min) min = v;
      if (v > max) max = v;
    }
    return { min, max: max > min ? max : min + 1 };
  }, [heatmapMaxSpeeds]);

  const smoothedWindowPositions = useMemo(
    () => smoothTrackPositions(windowPositions ?? []),
    [windowPositions]
  );
  const windowPoints = useMemo(
    () => simplifyTrack(smoothedWindowPositions, tolerance),
    [smoothedWindowPositions, tolerance]
  );

  const center = points[0] ?? [0, 0];

  return (
    <MapContainer center={center as L.LatLngExpression} zoom={14} className="h-full w-full">
      <ZoomTracker onZoom={setZoom} />
      <AutoInvalidateSize />
      <TileLayer
        key={tileUrl}
        url={tileUrl}
        attribution='&copy; OpenStreetMap &copy; CARTO'
      />
      {openSeaMap && (
        <TileLayer
          url="https://tiles.openseamap.org/seamark/{z}/{x}/{y}.png"
          attribution='&copy; OpenSeaMap'
          opacity={0.85}
        />
      )}

      {preRacePoints.length > 1 && (
        <Polyline positions={preRacePoints} pathOptions={{ color: "#B200FF", weight: 2, dashArray: "4,6", opacity: 0.85 }} />
      )}

      {trackMode === "flat" && points.length > 1 && (
        <Polyline positions={points} pathOptions={{ color: "#00FFFF", weight: 3, opacity: 0.9 }} />
      )}

      {trackMode === "heatmap" && heatmapPositions.length > 1 && (
        <>
          {heatmapPoints.slice(0, -1).map((p, i) => {
            const v = heatmapMaxSpeeds[i] ?? 0;
            const t = (v - speedRange.min) / (speedRange.max - speedRange.min);
            return (
              <Polyline
                key={i}
                positions={[p, heatmapPoints[i + 1]]}
                pathOptions={{ color: speedColor(Math.max(0, Math.min(1, t))), weight: 4, opacity: 0.9 }}
              />
            );
          })}
        </>
      )}

      {windowPoints.length > 1 && (
        <Polyline positions={windowPoints} pathOptions={{ color: "#FF8C00", weight: 9, opacity: 0.35 }} />
      )}

      {legs?.map((m, i) => (
        <CircleMarker
          key={i}
          center={[m.latitude, m.longitude]}
          radius={6}
          pathOptions={{ color: "#FFCC00", fillColor: "#FFCC00", fillOpacity: 0.8 }}
        >
          <Tooltip>{m.markName}</Tooltip>
        </CircleMarker>
      ))}

      {startLine?.pin && startLine?.boat && (
        <Polyline
          positions={[[startLine.pin.lat, startLine.pin.lon], [startLine.boat.lat, startLine.boat.lon]]}
          pathOptions={{ color: "#00CCFF", weight: 2, dashArray: "6,4", opacity: 0.9 }}
        />
      )}
      {startLine?.pin && (
        <Marker
          position={[startLine.pin.lat, startLine.pin.lon]}
          icon={L.divIcon({ className: "", html: '<div style="width:0;height:0;border-left:8px solid transparent;border-right:8px solid transparent;border-bottom:14px solid #00CCFF;"></div>', iconSize: [16, 14], iconAnchor: [8, 14] })}
        >
          <Tooltip>Pin end</Tooltip>
        </Marker>
      )}
      {startLine?.boat && (
        <Marker
          position={[startLine.boat.lat, startLine.boat.lon]}
          icon={L.divIcon({ className: "", html: '<div style="width:14px;height:14px;background:#FF4500;"></div>', iconSize: [14, 14], iconAnchor: [7, 7] })}
        >
          <Tooltip>Boat end</Tooltip>
        </Marker>
      )}

      {playbackPosition && (
        <Marker
          position={[playbackPosition.lat, playbackPosition.lon]}
          icon={arrowDivIcon(playbackPosition.cog)}
        />
      )}

      <FitBounds points={points as L.LatLngExpression[]} fitTick={fitTick} />
      <DragTracker onDrag={onExitFollow} />
      <FollowBoat point={playbackPosition ? [playbackPosition.lat, playbackPosition.lon] : null} followMode={followMode} />
    </MapContainer>
  );
}
