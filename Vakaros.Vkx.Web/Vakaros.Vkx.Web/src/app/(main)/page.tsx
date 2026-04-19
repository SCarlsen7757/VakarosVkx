"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { StatsSummary } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { useUnitPrefs } from "@/store/settings";
import {
  convertSpeed, convertDistance, formatDuration,
  speedUnitLabel, distanceUnitLabel,
} from "@/lib/units";
import { Card } from "@/components/ui/controls";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { ErrorBanner } from "@/components/ui/error-banner";
import { EmptyState } from "@/components/ui/empty-state";
import { Ship, Layers, Flag, Clock, Route, Gauge } from "lucide-react";

export default function DashboardPage() {
  const { prefs } = useUnitPrefs();
  const [stats, setStats] = useState<StatsSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    api.GET("/api/Stats/summary" as any, {}).then(({ data, error }: any) => {
      if (!mounted) return;
      if (error) setError("Failed to load summary");
      else setStats(data as StatsSummary);
      setLoading(false);
    }).catch(() => {
      if (!mounted) return;
      setError("Failed to load summary");
      setLoading(false);
    });
    return () => { mounted = false; };
  }, []);

  if (loading) {
    return (
      <div>
        <h1 className="mb-6 text-2xl font-bold">Dashboard</h1>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => <SkeletonLoader key={i} className="h-28" />)}
        </div>
      </div>
    );
  }

  if (error) return <ErrorBanner message={error} onRetry={() => location.reload()} />;
  if (!stats || n(stats.totalSessions) === 0) {
    return (
      <EmptyState
        title="No sessions yet"
        description="Upload your first .vkx file to get started."
        actionLabel="Upload session"
        actionHref="/upload"
      />
    );
  }

  const cards = [
    { label: "Boats", value: String(n(stats.totalBoats)), icon: Ship },
    { label: "Sessions", value: String(n(stats.totalSessions)), icon: Layers },
    { label: "Races", value: String(n(stats.totalRaces)), icon: Flag },
    { label: "Total session time", value: formatDuration(n(stats.totalSessionDurationSeconds)), icon: Clock },
    { label: "Distance sailed",
      value: `${convertDistance(n(stats.totalSailedDistanceMeters), prefs.course).toFixed(1)} ${distanceUnitLabel(prefs.course)}`,
      icon: Route },
    { label: "Top SOG",
      value: `${convertSpeed(n(stats.topSpeedOverGround), prefs.boatSpeed).toFixed(1)} ${speedUnitLabel(prefs.boatSpeed)}`,
      icon: Gauge },
  ];

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Dashboard</h1>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map((c) => (
          <Card key={c.label} className="p-5">
            <div className="flex items-start justify-between">
              <div>
                <div className="text-xs uppercase tracking-wider text-text-secondary">{c.label}</div>
                <div className="mt-2 font-mono text-2xl font-semibold text-text-primary">{c.value}</div>
              </div>
              <c.icon className="h-6 w-6 text-action-primary" />
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
