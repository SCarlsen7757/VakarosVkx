"use client";

import { useEffect, useRef } from "react";
import { Play, Pause } from "lucide-react";
import { useRaceViewerStore } from "@/store/race-viewer";
import { formatSignedClock } from "@/lib/units";

export function PlaybackControls({ raceStartOffset, duration }: { raceStartOffset: number; duration: number }) {
  const {
    isPlaying, togglePlay, speed, setSpeed,
    position, setPosition,
    windowEnd,
    mode, setMode, setDuration: setDur, setRaceStartOffset, setWindow,
  } = useRaceViewerStore();

  useEffect(() => {
    setDur(duration);
    setRaceStartOffset(raceStartOffset);
    setWindow(0, duration);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [duration, raceStartOffset]);

  const tickRef = useRef<number | null>(null);
  const lastRef = useRef<number>(0);
  useEffect(() => {
    if (!isPlaying) {
      if (tickRef.current) cancelAnimationFrame(tickRef.current);
      return;
    }
    lastRef.current = performance.now();
    const loop = (now: number) => {
      const dt = (now - lastRef.current) / 1000;
      lastRef.current = now;
      const current = useRaceViewerStore.getState();
      const next = current.position + dt * speed;
      if (next >= current.windowEnd) {
        setPosition(current.windowEnd);
        useRaceViewerStore.setState({ isPlaying: false });
        return;
      }
      setPosition(next);
      tickRef.current = requestAnimationFrame(loop);
    };
    tickRef.current = requestAnimationFrame(loop);
    return () => { if (tickRef.current) cancelAnimationFrame(tickRef.current); };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isPlaying, speed]);

  const elapsed = position - raceStartOffset;
  const timerColor = elapsed < 0 ? "text-warning" : "text-success";

  return (
    <div className="space-y-3 rounded-lg bg-bg-surface p-4 ring-1 ring-border-default">
      <div className="flex flex-wrap items-center gap-4">
        <div className={`font-mono text-3xl font-semibold ${timerColor}`}>
          {formatSignedClock(elapsed)}
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={togglePlay}
            className="rounded-full bg-action-primary p-2 text-white hover:bg-action-hover"
            aria-label={isPlaying ? "Pause" : "Play"}
          >
            {isPlaying ? <Pause className="h-5 w-5" /> : <Play className="h-5 w-5" />}
          </button>
          <select
            value={speed}
            onChange={(e) => setSpeed(Number(e.target.value) as 0.5 | 1 | 2 | 4)}
            className="rounded border border-border-default bg-bg-base px-2 py-1 text-sm"
          >
            <option value={0.5}>0.5×</option>
            <option value={1}>1×</option>
            <option value={2}>2×</option>
            <option value={4}>4×</option>
          </select>
        </div>
        <div className="ml-auto inline-flex rounded-md ring-1 ring-border-default">
          <button
            onClick={() => setMode("historical")}
            className={`px-3 py-1 text-xs ${mode === "historical" ? "bg-action-primary text-white" : "text-text-secondary"}`}
          >Historical</button>
          <button
            onClick={() => setMode("current")}
            className={`px-3 py-1 text-xs ${mode === "current" ? "bg-action-primary text-white" : "text-text-secondary"}`}
          >Current</button>
        </div>
      </div>

      <div>
        <div className="mb-1 flex justify-between text-xs text-text-secondary">
          <span>Position</span>
          <span className="font-mono">{position.toFixed(1)}s / {duration.toFixed(0)}s</span>
        </div>
        <input
          type="range"
          min={0}
          max={duration}
          step={0.1}
          value={position}
          onChange={(e) => setPosition(Number(e.target.value))}
          className="w-full accent-[color:var(--action-primary)]"
        />
      </div>
    </div>
  );
}
