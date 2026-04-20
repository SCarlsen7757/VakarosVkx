"use client";

import { use, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { RaceMap } from "@/components/map/race-map";
import { TelemetryPanels } from "@/components/charts/telemetry-panels";
import { PlaybackControls } from "@/components/race-viewer/playback-controls";
import { StartAnalysisPanel } from "@/components/race-viewer/start-analysis-panel";
import { AiSummary } from "@/components/race-viewer/ai-summary";
import { CompassRose, HeelTrimCard, NumericGauge } from "@/components/gauges/gauges";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { ErrorBanner } from "@/components/ui/error-banner";
import type { RaceDetail, Position, Course } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { interpolatePosition } from "@/lib/track-utils";
import { useUnitPrefs } from "@/store/settings";
import { convertSpeed, radiansToDegrees, speedUnitLabel } from "@/lib/units";
import { useRaceViewerStore } from "@/store/race-viewer";

interface PageProps { params: Promise<{ id: string; raceNumber: string }>; }

function quatToHeelTrim(w: number, x: number, y: number, z: number) {
  const sinr_cosp = 2 * (w * x + y * z);
  const cosr_cosp = 1 - 2 * (x * x + y * y);
  const roll = Math.atan2(sinr_cosp, cosr_cosp);
  const sinp = 2 * (w * y - z * x);
  const pitch = Math.abs(sinp) >= 1 ? Math.sign(sinp) * Math.PI / 2 : Math.asin(sinp);
  return { heel: radiansToDegrees(roll), trim: radiansToDegrees(pitch) };
}

export default function RaceViewerPage({ params }: PageProps) {
  const { id, raceNumber } = use(params);
  const raceNum = Number(raceNumber);
  const { prefs } = useUnitPrefs();
  const position = useRaceViewerStore((s) => s.position);
  const mode = useRaceViewerStore((s) => s.mode);

  const [race, setRace] = useState<RaceDetail | null>(null);
  const [positions, setPositions] = useState<Position[] | null>(null);
  const [course, setCourse] = useState<Course | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    const base = `/api/sessions/${id}/races/${raceNum}`;
    fetch(base)
      .then((r) => r.ok ? r.json() as Promise<RaceDetail> : Promise.reject(r.status))
      .then(async (raceData) => {
        if (!alive) return;
        setRace(raceData);
        const countdown = raceData.countdownDurationSeconds != null ? n(raceData.countdownDurationSeconds) : 0;
        const fromParam = countdown > 0 ? `?from=${-countdown}` : "";
        const [posData, courseData] = await Promise.all([
          fetch(`${base}/positions${fromParam}`).then((r) => r.ok ? r.json() as Promise<Position[]> : Promise.reject(r.status)),
          raceData.courseId != null
            ? fetch(`/api/Courses/${raceData.courseId}`).then((r) => r.ok ? r.json() : null)
            : Promise.resolve(null),
        ]);
        if (!alive) return;
        setPositions(posData);
        setCourse(courseData);
      })
      .catch((e) => alive && setError(`Failed to load race (${e})`));
    return () => { alive = false; };
  }, [id, raceNum]);

  const startMs = race ? new Date(race.startedAt).getTime() : 0;
  const duration = race ? n(race.durationSeconds) : 0;
  const raceStartOffset = race?.countdownDurationSeconds != null ? n(race.countdownDurationSeconds) : 0;
  const totalDuration = raceStartOffset + duration;

  const racePositions = useMemo(() => positions?.filter((p) => new Date(p.time).getTime() >= startMs) ?? null, [positions, startMs]);
  const preRacePositions = useMemo(() => positions?.filter((p) => new Date(p.time).getTime() < startMs) ?? null, [positions, startMs]);

  // Resolve current position (snap to nearest) — used for gauges and heel/trim.
  const targetMs = startMs + (position - raceStartOffset) * 1000;
  const currentPos = useMemo(() => {
    if (!positions || !positions.length) return null;
    let lo = 0, hi = positions.length - 1, best = 0, bestD = Infinity;
    while (lo <= hi) {
      const mid = (lo + hi) >> 1;
      const t = new Date(positions[mid].time).getTime();
      const d = Math.abs(t - targetMs);
      if (d < bestD) { bestD = d; best = mid; }
      if (t < targetMs) lo = mid + 1; else hi = mid - 1;
    }
    return positions[best];
  }, [positions, targetMs]);

  // Interpolated position for the map boat arrow — smooth between GPS samples.
  const playbackArrow = useMemo(() => {
    if (!positions || !positions.length) return null;
    const interp = interpolatePosition(positions, targetMs);
    if (!interp) return null;
    return { lat: interp.lat, lon: interp.lon, cog: radiansToDegrees(interp.cog) };
  }, [positions, targetMs]);

  const heelTrim = currentPos ? quatToHeelTrim(n(currentPos.quaternionW), n(currentPos.quaternionX), n(currentPos.quaternionY), n(currentPos.quaternionZ)) : null;

  const startLine = race && race.pinEnd && race.boatEnd ? {
    pin: { lat: n(race.pinEnd.latitude), lon: n(race.pinEnd.longitude) },
    boat: { lat: n(race.boatEnd.latitude), lon: n(race.boatEnd.longitude) },
  } : undefined;

  const legs = course?.legs.map((l) => ({ latitude: n(l.latitude), longitude: n(l.longitude), markName: l.markName })) ?? [];

  if (error) return <ErrorBanner message={error} />;
  if (!race || !positions) return <SkeletonLoader className="h-96" />;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Link href={`/sessions/${id}`} className="inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary">
          <ArrowLeft className="h-4 w-4" /> Back to session
        </Link>
        <h1 className="text-2xl font-semibold">Race {race.raceNumber}</h1>
        {course && <span className="text-text-secondary">· {course.name}</span>}
      </div>

      <PlaybackControls raceStartOffset={raceStartOffset} duration={totalDuration} />

      <RaceMap
        race={race}
        positions={racePositions}
        preRacePositions={preRacePositions}
        legs={legs}
        startLine={startLine}
        playbackPosition={playbackArrow}
      />

      {mode === "current" && (
      <div className="grid grid-cols-3 gap-3">
        <NumericGauge label="SOG" value={currentPos ? convertSpeed(n(currentPos.speedOverGround), prefs.boatSpeed) : null} unit={speedUnitLabel(prefs.boatSpeed)} big />
        <CompassRose headingDeg={currentPos ? radiansToDegrees(n(currentPos.courseOverGround)) : null} />
        <HeelTrimCard heel={heelTrim?.heel ?? null} trim={heelTrim?.trim ?? null} />
      </div>
      )}

      <StartAnalysisPanel data={race.startAnalysis} sessionId={id} raceNumber={raceNum} />

      {mode === "historical" && <TelemetryPanels sessionId={id} raceNumber={raceNum} />}

      <AiSummary sessionId={id} raceNumber={raceNum} />
    </div>
  );
}
