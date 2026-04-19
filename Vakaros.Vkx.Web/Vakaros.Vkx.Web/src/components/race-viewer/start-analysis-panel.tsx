"use client";

import type { StartAnalysis } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { Card } from "@/components/ui/controls";
import { useUnitPrefs } from "@/store/settings";
import { convertSpeed, speedUnitLabel } from "@/lib/units";

export function StartAnalysisPanel({ data }: { data: StartAnalysis | null | undefined }) {
  const { prefs } = useUnitPrefs();
  if (!data) return null;
  const bias = n(data.timeBiasSeconds);
  const biasColor = bias > 0 ? "text-warning" : "text-success";
  return (
    <Card className="p-4">
      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wider text-text-secondary">Start analysis</h3>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-5">
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
          <div className="text-xs text-text-secondary">Line fraction</div>
          <div className="font-mono text-lg">{(n(data.lineFraction) * 100).toFixed(0)}%</div>
        </div>
        <div>
          <div className="text-xs text-text-secondary">Crossed at</div>
          <div className="font-mono text-sm">{new Date(data.crossedAt).toLocaleTimeString()}</div>
        </div>
      </div>
    </Card>
  );
}
