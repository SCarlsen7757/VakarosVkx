import simplify from "simplify-js";
import type { Position } from "@/lib/schemas";
import { n } from "@/lib/schemas";

/**
 * Maps a Leaflet zoom level to an RDP tolerance in degrees.
 * Lower zoom (zoomed out) → higher tolerance (fewer points, straighter lines).
 * Higher zoom (zoomed in) → lower tolerance (more points, more detail).
 */
export function zoomToTolerance(zoom: number): number {
  if (zoom <= 10) return 0.001;
  if (zoom === 11) return 0.0005;
  if (zoom === 12) return 0.0003;
  if (zoom === 13) return 0.0001;
  if (zoom === 14) return 0.00005;
  if (zoom === 15) return 0.00002;
  return 0.00001;
}

/**
 * Applies a sliding-window average to the latitude and longitude of GPS positions.
 * This rounds off artificially sharp tacks/gybes caused by point reduction while
 * preserving all other fields (time, speed, COG, etc.) from the central sample.
 *
 * @param positions  Raw GPS position array
 * @param windowSize Number of samples to average. Must be odd; defaults to 5.
 */
export function smoothTrackPositions(positions: Position[], windowSize = 5): Position[] {
  if (positions.length < 2) return positions;
  const half = Math.floor(windowSize / 2);
  return positions.map((p, i) => {
    const start = Math.max(0, i - half);
    const end = Math.min(positions.length - 1, i + half);
    let sumLat = 0, sumLon = 0, count = 0;
    for (let j = start; j <= end; j++) {
      sumLat += n(positions[j].latitude);
      sumLon += n(positions[j].longitude);
      count++;
    }
    return { ...p, latitude: sumLat / count, longitude: sumLon / count };
  });
}

/**
 * Simplifies a GPS track using the Ramer-Douglas-Peucker algorithm (via simplify-js).
 * Returns [lat, lon] pairs suitable for Leaflet Polyline rendering.
 *
 * @param positions  Raw GPS position array (unsimplified, with timestamps)
 * @param tolerance  RDP tolerance in degrees. Default 0.00005° ≈ 5 m.
 */
export function simplifyTrack(
  positions: Position[],
  tolerance = 0.00005
): [number, number][] {
  if (positions.length < 2) {
    return positions.map((p) => [n(p.latitude), n(p.longitude)]);
  }
  const pts = positions.map((p) => ({ x: n(p.longitude), y: n(p.latitude) }));
  const simplified = simplify(pts, tolerance, true);
  return simplified.map((p) => [p.y, p.x]);
}

/**
 * Simplifies a GPS track and returns the subset of original Position objects
 * whose coordinates were retained by RDP.  Useful when you need per-point
 * metadata (e.g. speed) alongside the simplified geometry.
 *
 * @param positions  Raw GPS position array
 * @param tolerance  RDP tolerance in degrees (default 0.00005° ≈ 5 m)
 */
export function simplifyPositions(
  positions: Position[],
  tolerance = 0.00005
): Position[] {
  if (positions.length < 2) return positions;
  const pts = positions.map((p) => ({ x: n(p.longitude), y: n(p.latitude) }));
  const simplified = simplify(pts, tolerance, true);
  // Build a Set of "lon_lat" keys for O(1) lookup
  const kept = new Set(simplified.map((p) => `${p.x}_${p.y}`));
  return positions.filter((p) => kept.has(`${n(p.longitude)}_${n(p.latitude)}`));
}

export interface InterpolatedPosition {
  lat: number;
  lon: number;
  /** Course over ground in radians */
  cog: number;
}

/**
 * Interpolates the boat position and heading between the two GPS samples that
 * bracket `targetMs`.  Returns null when the positions array is empty or
 * targetMs is out of range.
 *
 * - lat/lon: linear interpolation
 * - COG: angular shortest-path interpolation (handles 0/2π wrap-around)
 */
export function interpolatePosition(
  positions: Position[],
  targetMs: number
): InterpolatedPosition | null {
  if (!positions.length) return null;

  // Binary search: find the last index whose timestamp <= targetMs
  let lo = 0;
  let hi = positions.length - 1;

  const tFirst = new Date(positions[0].time).getTime();
  const tLast = new Date(positions[hi].time).getTime();

  if (targetMs <= tFirst) {
    const p = positions[0];
    return { lat: n(p.latitude), lon: n(p.longitude), cog: n(p.courseOverGround) };
  }
  if (targetMs >= tLast) {
    const p = positions[hi];
    return { lat: n(p.latitude), lon: n(p.longitude), cog: n(p.courseOverGround) };
  }

  while (lo < hi - 1) {
    const mid = (lo + hi) >> 1;
    if (new Date(positions[mid].time).getTime() <= targetMs) {
      lo = mid;
    } else {
      hi = mid;
    }
  }

  const p0 = positions[lo];
  const p1 = positions[hi];
  const t0 = new Date(p0.time).getTime();
  const t1 = new Date(p1.time).getTime();
  const frac = t1 === t0 ? 0 : (targetMs - t0) / (t1 - t0);

  const lat = n(p0.latitude) + (n(p1.latitude) - n(p0.latitude)) * frac;
  const lon = n(p0.longitude) + (n(p1.longitude) - n(p0.longitude)) * frac;

  // Angular shortest-path interpolation for COG (radians)
  const cog0 = n(p0.courseOverGround);
  const cog1 = n(p1.courseOverGround);
  let delta = cog1 - cog0;
  while (delta > Math.PI) delta -= 2 * Math.PI;
  while (delta < -Math.PI) delta += 2 * Math.PI;
  const cog = cog0 + delta * frac;

  return { lat, lon, cog };
}
