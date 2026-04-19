"use client";

import { useEffect, useMemo, useRef } from "react";
import { MapContainer, TileLayer, Polyline, CircleMarker, Marker, Tooltip, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { useTheme } from "next-themes";
import { n } from "@/lib/schemas";
import type { RaceMapProps, TrackMode } from "./race-map";

interface InternalProps extends RaceMapProps {
  openSeaMap: boolean;
  trackMode: TrackMode;
  recenterTick: number;
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

function FitBounds({ points, fitTick }: { points: L.LatLngExpression[]; fitTick: number }) {
  const map = useMap();
  useEffect(() => {
    if (points.length === 0) return;
    const b = L.latLngBounds(points);
    map.fitBounds(b.pad(0.05));
  }, [fitTick, points, map]);
  return null;
}

function Recenter({ point, tick }: { point: L.LatLngExpression | null; tick: number }) {
  const map = useMap();
  useEffect(() => {
    if (point) map.panTo(point);
  }, [tick, point, map]);
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
  positions, race, legs, startLine, playbackPosition, preRacePositions,
  openSeaMap, trackMode, recenterTick, fitTick,
}: InternalProps) {
  const { resolvedTheme } = useTheme();
  const tileUrl = resolvedTheme === "dark"
    ? "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
    : "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png";
  const points = useMemo(
    () => (positions ?? []).map((p) => [n(p.latitude), n(p.longitude)] as [number, number]),
    [positions]
  );
  const preRacePoints = useMemo(
    () => (preRacePositions ?? []).map((p) => [n(p.latitude), n(p.longitude)] as [number, number]),
    [preRacePositions]
  );

  const speedRange = useMemo(() => {
    if (!positions || positions.length === 0) return { min: 0, max: 1 };
    let min = Infinity, max = -Infinity;
    for (const p of positions) {
      const v = n(p.speedOverGround);
      if (v < min) min = v;
      if (v > max) max = v;
    }
    return { min, max: max > min ? max : min + 1 };
  }, [positions]);

  const center = points[0] ?? [0, 0];

  return (
    <MapContainer center={center as L.LatLngExpression} zoom={14} className="h-full w-full">
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

      {trackMode === "heatmap" && positions && positions.length > 1 && (
        <>
          {points.slice(0, -1).map((p, i) => {
            const v = n(positions[i].speedOverGround);
            const t = (v - speedRange.min) / (speedRange.max - speedRange.min);
            return (
              <Polyline
                key={i}
                positions={[p, points[i + 1]]}
                pathOptions={{ color: speedColor(Math.max(0, Math.min(1, t))), weight: 4, opacity: 0.9 }}
              />
            );
          })}
        </>
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
      <Recenter point={playbackPosition ? [playbackPosition.lat, playbackPosition.lon] : null} tick={recenterTick} />
    </MapContainer>
  );
}
