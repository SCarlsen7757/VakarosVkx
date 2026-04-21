"use client";

import { use, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { RaceMap } from "@/components/map/race-map";
import { TelemetryPanels } from "@/components/charts/telemetry-panels";
import { TimeWindowSlicer } from "@/components/charts/time-window-slicer";
import { PlaybackControls } from "@/components/race-viewer/playback-controls";
import { CompassRose, Inclinometer, NumericGauge } from "@/components/gauges/gauges";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { ErrorBanner } from "@/components/ui/error-banner";
import type { SessionDetail, Position } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { useUnitPrefs } from "@/store/settings";
import { convertSpeed, radiansToDegrees, speedUnitLabel } from "@/lib/units";
import { useRaceViewerStore } from "@/store/race-viewer";

interface PageProps { params: Promise<{ id: string }>; }

function quatToHeelTrim(w: number, x: number, y: number, z: number) {
  const sinr_cosp = 2 * (w * x + y * z);
  const cosr_cosp = 1 - 2 * (x * x + y * y);
  const roll = Math.atan2(sinr_cosp, cosr_cosp);
  const sinp = 2 * (w * y - z * x);
  const pitch = Math.abs(sinp) >= 1 ? Math.sign(sinp) * Math.PI / 2 : Math.asin(sinp);
  return { heel: radiansToDegrees(roll), trim: radiansToDegrees(pitch) };
}

export default function SessionViewerPage({ params }: PageProps) {
  const { id } = use(params);
  const { prefs } = useUnitPrefs();
  const position = useRaceViewerStore((s) => s.position);
  const showGauges = useRaceViewerStore((s) => s.showGauges);
  const showCharts = useRaceViewerStore((s) => s.showCharts);
  const windowStart = useRaceViewerStore((s) => s.windowStart);
  const windowEnd = useRaceViewerStore((s) => s.windowEnd);

  const [session, setSession] = useState<SessionDetail | null>(null);
  const [positions, setPositions] = useState<Position[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    Promise.all([
      fetch(`/api/Sessions/${id}`).then((r) => r.ok ? r.json() : Promise.reject(r.status)),
      // session viewer reuses race route #1 if available; otherwise we approximate
      fetch(`/api/sessions/${id}/races/1/positions`).then((r) => r.ok ? r.json() : []),
    ])
      .then(([s, p]: [SessionDetail, Position[]]) => {
        if (!alive) return;
        setSession(s);
        setPositions(p ?? []);
      })
      .catch((e) => alive && setError(`Failed to load session (${e})`));
    return () => { alive = false; };
  }, [id]);

  const startMs = session ? new Date(session.startedAt).getTime() : 0;
  const endMs = session ? new Date(session.endedAt).getTime() : 0;
  const duration = (endMs - startMs) / 1000;

  const targetMs = startMs + position * 1000;
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

  const playbackArrow = currentPos ? { lat: n(currentPos.latitude), lon: n(currentPos.longitude), cog: n(currentPos.courseOverGround) } : null;
  const heelTrim = currentPos ? quatToHeelTrim(n(currentPos.quaternionW), n(currentPos.quaternionX), n(currentPos.quaternionY), n(currentPos.quaternionZ)) : null;

  // Session viewer has no countdown — raceStartOffset is 0.
  // Only show highlight in Historical mode when the window is narrowed (not covering full data range).
  const windowStartMs = startMs + windowStart * 1000;
  const windowEndMs = startMs + windowEnd * 1000;
  const isWindowNarrowed = windowStart > 0 || windowEnd < duration;
  const windowPositions = useMemo(
    () => (showCharts && isWindowNarrowed)
      ? positions?.filter((p) => {
          const t = new Date(p.time).getTime();
          return t >= windowStartMs && t <= windowEndMs;
        }) ?? null
      : null,
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [positions, windowStartMs, windowEndMs, showCharts, isWindowNarrowed]
  );

  if (error) return <ErrorBanner message={error} />;
  if (!session) return <SkeletonLoader className="h-96" />;

  const hasRightContent = showGauges || showCharts;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Link href={`/sessions/${id}`} className="inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary">
          <ArrowLeft className="h-4 w-4" /> Back to session
        </Link>
        <h1 className="text-2xl font-semibold">Session viewer</h1>
        <span className="text-text-secondary">· {session.fileName}</span>
      </div>

      <div className={`flex flex-col gap-4 ${hasRightContent ? "lg:flex-row lg:items-start" : ""}`}>
        {/* Left column: playback controls + map */}
        <div className={`flex flex-col gap-4 ${hasRightContent ? "lg:w-[42%] lg:shrink-0 lg:sticky lg:top-4 lg:self-start" : "w-full"}`}>
          <PlaybackControls raceStartOffset={0} duration={Math.max(duration, 0)} />
          <RaceMap race={null} positions={positions} playbackPosition={playbackArrow} windowPositions={windowPositions} square={hasRightContent} />
        </div>

        {hasRightContent && (
          <div className="flex flex-col gap-4 flex-1 min-w-0 lg:overflow-y-auto lg:max-h-[calc(100vh-5rem)]">
            {showGauges && (
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                <NumericGauge label="SOG" value={currentPos ? convertSpeed(n(currentPos.speedOverGround), prefs.boatSpeed) : null} unit={speedUnitLabel(prefs.boatSpeed)} big />
                <CompassRose headingDeg={currentPos ? n(currentPos.courseOverGround) : null} />
                <Inclinometer label="Heel (°)" value={heelTrim?.heel ?? null} range={45} />
                <Inclinometer label="Trim (°)" value={heelTrim?.trim ?? null} range={10} />
              </div>
            )}

            {showCharts && session.races.length > 0 && (
              <>
                <TimeWindowSlicer raceStartOffset={0} />
                <TelemetryPanels sessionId={id} raceNumber={n(session.races[0].raceNumber)} raceStartMs={startMs} raceStartOffset={0} />
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
