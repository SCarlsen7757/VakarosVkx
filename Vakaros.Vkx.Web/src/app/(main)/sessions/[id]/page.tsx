"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import type { SessionDetail, Boat, Course, Race } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { formatDuration } from "@/lib/units";
import { Button, Card, Select } from "@/components/ui/controls";
import { ErrorBanner } from "@/components/ui/error-banner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { useToast } from "@/hooks/useToast";
import { ChevronRight } from "lucide-react";

export default function SessionDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const toast = useToast();
  const [session, setSession] = useState<SessionDetail | null>(null);
  const [boats, setBoats] = useState<Boat[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [boatId, setBoatId] = useState<string>("");
  const [raceCourses, setRaceCourses] = useState<Record<string, string>>({});
  const [confirm, setConfirm] = useState(false);

  const load = () => {
    api.GET(`/api/v1/sessions/{id}` as any, { params: { path: { id } } } as any).then(({ data, error }: any) => {
      if (error) setError("Failed to load session");
      else {
        const s = data as SessionDetail;
        setSession(s);
        setBoatId(String(s.boatId ?? ""));
        const initial: Record<string, string> = {};
        s.races.forEach((r) => { initial[String(r.raceNumber)] = String(r.courseId ?? ""); });
        setRaceCourses(initial);
      }
    });
  };

  useEffect(() => {
    load();
    api.GET("/api/v1/boats" as any, {} as any).then(({ data }: any) => setBoats((data as Boat[]) ?? []));
    api.GET("/api/v1/courses" as any, {} as any).then(({ data }: any) => setCourses((data as Course[]) ?? []));
  }, [id]);

  const saveBoat = async () => {
    const res = await fetch(`/api/v1/sessions/${id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ boatId: boatId ? Number(boatId) : null }),
    });
    if (res.ok) { toast.push({ kind: "success", message: "Boat assigned." }); load(); }
    else toast.push({ kind: "error", message: "Failed to update session." });
  };

  const saveRaceCourse = async (race: Race) => {
    const courseId = raceCourses[String(race.raceNumber)];
    const res = await fetch(`/api/v1/sessions/${id}/races/${race.raceNumber}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ courseId: courseId ? Number(courseId) : null }),
    });
    if (res.ok) { toast.push({ kind: "success", message: `Race ${race.raceNumber} updated.` }); load(); }
    else toast.push({ kind: "error", message: "Failed to update race." });
  };

  const doDelete = async () => {
    const res = await fetch(`/api/v1/sessions/${id}`, { method: "DELETE" });
    setConfirm(false);
    if (res.ok || res.status === 204) {
      toast.push({ kind: "success", message: "Session deleted." });
      router.push("/sessions");
    } else toast.push({ kind: "error", message: "Failed to delete." });
  };

  if (error) return <ErrorBanner message={error} onRetry={load} />;
  if (!session) return <div className="space-y-2">{Array.from({ length: 5 }).map((_, i) => <SkeletonLoader key={i} className="h-12" />)}</div>;

  const dur = (new Date(session.endedAt).getTime() - new Date(session.startedAt).getTime()) / 1000;

  return (
    <div className="space-y-6">
      <nav className="flex items-center gap-1 text-sm text-text-secondary">
        <Link href="/sessions" className="hover:text-text-primary">Sessions</Link>
        <ChevronRight className="h-4 w-4" />
        <span className="text-text-primary">{session.fileName}</span>
      </nav>

      <Card className="p-5">
        <h2 className="mb-4 text-lg font-semibold">Session metadata</h2>
        <dl className="grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
          <div><dt className="text-text-secondary">File</dt><dd>{session.fileName}</dd></div>
          <div><dt className="text-text-secondary">Format</dt><dd>v{n(session.formatVersion)} @ {n(session.telemetryRateHz)} Hz</dd></div>
          <div><dt className="text-text-secondary">Started</dt><dd>{new Date(session.startedAt).toLocaleString()}</dd></div>
          <div><dt className="text-text-secondary">Duration</dt><dd className="font-mono">{formatDuration(dur)}</dd></div>
          <div><dt className="text-text-secondary">Body frame</dt><dd>{session.isFixedToBodyFrame ? "Yes" : "No"}</dd></div>
          <div><dt className="text-text-secondary">Uploaded</dt><dd>{new Date(session.uploadedAt).toLocaleString()}</dd></div>
        </dl>

        <div className="mt-5 flex flex-wrap items-end gap-3">
          <label className="block w-full max-w-xs">
            <span className="text-sm text-text-secondary">Boat</span>
            <Select value={boatId} onChange={(e) => setBoatId(e.target.value)}>
              <option value="">— Unassigned —</option>
              {boats.map((b) => <option key={String(b.id)} value={String(b.id)}>{b.name}</option>)}
            </Select>
          </label>
          <Button onClick={saveBoat}>Save</Button>
          <Link
            href={`/sessions/${id}/viewer`}
            className="ml-auto inline-flex items-center rounded-md border border-border-default bg-bg-surface px-3 py-1.5 text-sm font-medium hover:bg-bg-elevated"
          >
            View session data
          </Link>
        </div>
      </Card>

      <Card className="overflow-hidden">
        <h2 className="px-5 pt-4 text-lg font-semibold">Races</h2>
        {session.races.length === 0 ? (
          <p className="px-5 py-8 text-sm text-text-secondary">No races detected in this session.</p>
        ) : (
          <table className="w-full">
            <thead className="bg-bg-elevated text-xs uppercase tracking-wider text-text-secondary">
              <tr>
                <th className="px-3 py-2 text-left">Race #</th>
                <th className="px-3 py-2 text-left">Started</th>
                <th className="px-3 py-2 text-left">Ended</th>
                <th className="px-3 py-2 text-left">Duration</th>
                <th className="px-3 py-2 text-left">Course</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {session.races.map((r) => (
                <tr key={String(r.raceNumber)} className="border-t border-border-default text-sm">
                  <td className="px-3 py-2">
                    <Link className="text-action-primary hover:underline" href={`/sessions/${id}/races/${r.raceNumber}`}>
                      Race {n(r.raceNumber)}
                    </Link>
                  </td>
                  <td className="px-3 py-2 text-text-secondary">{new Date(r.startedAt).toLocaleString()}</td>
                  <td className="px-3 py-2 text-text-secondary">{r.endedAt ? new Date(r.endedAt).toLocaleString() : "—"}</td>
                  <td className="px-3 py-2 font-mono">{r.durationSeconds != null ? formatDuration(n(r.durationSeconds)) : "—"}</td>
                  <td className="px-3 py-2">
                    <Select
                      value={raceCourses[String(r.raceNumber)] ?? ""}
                      onChange={(e) => setRaceCourses({ ...raceCourses, [String(r.raceNumber)]: e.target.value })}
                    >
                      <option value="">— None —</option>
                      {courses.map((c) => <option key={String(c.id)} value={String(c.id)}>{c.name}</option>)}
                    </Select>
                  </td>
                  <td className="px-3 py-2"><Button variant="secondary" onClick={() => saveRaceCourse(r)}>Save</Button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>

      <div className="flex justify-end">
        <Button variant="danger" onClick={() => setConfirm(true)}>Delete session</Button>
      </div>

      <ConfirmDialog
        open={confirm}
        title="Delete session"
        message="This will permanently delete the session and all derived data."
        destructive
        confirmLabel="Delete"
        onConfirm={doDelete}
        onCancel={() => setConfirm(false)}
      />
    </div>
  );
}
