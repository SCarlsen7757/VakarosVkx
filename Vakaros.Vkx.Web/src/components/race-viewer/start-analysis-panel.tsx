"use client";

import { useEffect, useState } from "react";
import type { StartAnalysis } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { Card } from "@/components/ui/controls";
import { useUnitPrefs } from "@/store/settings";
import { convertSpeed, speedUnitLabel } from "@/lib/units";

interface Props {
  data: StartAnalysis | null | undefined;
  sessionId: string | number;
  raceNumber: number;
}

export function StartAnalysisPanel({ data, sessionId, raceNumber }: Props) {
  const { prefs } = useUnitPrefs();
  const [lineLength, setLineLength] = useState<number | null>(null);

  useEffect(() => {
    if (!data) return;
    fetch(`/api/sessions/${sessionId}/races/${raceNumber}/start-line-length`)
      .then((r) => r.ok ? r.json() : null)
      .then((v) => setLineLength(v != null && typeof v.lengthMeters !== "undefined" ? parseFloat(v.lengthMeters) : null))
      .catch(() => null);
  }, [sessionId, raceNumber, data]);

  if (!data) return null;
  const bias = n(data.timeBiasSeconds);
  const biasColor = bias > 0 ? "text-warning" : "text-success";
  const fractionMeters = lineLength != null ? (n(data.lineFraction) * lineLength).toFixed(1) + " m" : null;
  return (
    <Card className="p-4">
      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wider text-text-secondary">Start analysis</h3>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-6">
        <div>
          <div className="text-xs text-text-secondary">Time bias</div>
          <div className={`font-mono text-2xl font-semibold ${biasColor}`}>{bias > 0 ? "+" : ""}{bias.toFixed(1)}s</div>
        </div>
        <div>
          <div className="text-xs text-text-secondary">Crossing speed</div>
          <div className="font-mono text-lg">{convertSpeed(n(data.speedAtCrossingMs), prefs.boatSpeed).toFixed(1)} {speedUnitLabel(prefs.boatSpeed)}</div>
        </div>
        <div>
          <div className="text-xs text-text-secondary">Approach</div>
          <div className="font-mono text-lg">{n(data.approachCourseDegrees).toFixed(0)}°</div>
        </div>
        <div>
          <div className="text-xs text-text-secondary">Position on line</div>
          <div className="font-mono text-lg">{fractionMeters ?? "—"}</div>
        </div>
        <div>
          <div className="text-xs text-text-secondary">Line length</div>
          <div className="font-mono text-lg">{lineLength != null ? lineLength.toFixed(1) + " m" : "—"}</div>
        </div>
        <div>
          <div className="text-xs text-text-secondary">Crossed at</div>
          <div className="font-mono text-sm">{new Date(data.crossedAt).toLocaleTimeString()}</div>
        </div>
      </div>
    </Card>
  );
}
