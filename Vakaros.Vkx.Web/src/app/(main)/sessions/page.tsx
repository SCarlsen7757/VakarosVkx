"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import type { SessionSummary, Boat } from "@/lib/schemas";
import { n } from "@/lib/schemas";
import { formatDuration } from "@/lib/units";
import { Card, Input } from "@/components/ui/controls";
import { ErrorBanner } from "@/components/ui/error-banner";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { ThreeDotMenu } from "@/components/ui/three-dot-menu";
import { PaginationControls } from "@/components/ui/pagination-controls";
import { SortableTableHeader, SortDir } from "@/components/ui/sortable-table-header";
import { FilterToolbar, SearchInput } from "@/components/ui/filter-toolbar";
import { useToast } from "@/hooks/useToast";
import { cn } from "@/lib/cn";

type SortField = "fileName" | "boatName" | "courseName" | "startedAt" | "durationSeconds" | "raceCount";
type VisibilityFilter = "mine" | "team" | "public";

function SessionBadges({ session }: { session: SessionSummary }) {
  return (
    <span className="ml-1 inline-flex gap-1">
      {session.sharedViaTeams?.map((t) => (
        <span key={t} className="rounded bg-blue-500/15 px-1.5 py-0.5 text-[10px] font-medium text-blue-400">{t}</span>
      ))}
      {session.isPublic && (
        <span className="rounded bg-green-500/15 px-1.5 py-0.5 text-[10px] font-medium text-green-400">Public</span>
      )}
    </span>
  );
}

export default function SessionsPage() {
  const toast = useToast();
  const [sessions, setSessions] = useState<SessionSummary[] | null>(null);
  const [boats, setBoats] = useState<Boat[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [boatFilter, setBoatFilter] = useState<string>("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [sortField, setSortField] = useState<SortField | null>("startedAt");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [confirmDelete, setConfirmDelete] = useState<SessionSummary | null>(null);
  const [activeFilters, setActiveFilters] = useState<Set<VisibilityFilter>>(new Set(["mine", "team", "public"]));

  const refresh = () => {
    api.GET("/api/v1/sessions" as any, {} as any).then(({ data, error }: any) => {
      if (error) setError("Failed to load sessions");
      else setSessions(data as SessionSummary[]);
    });
  };

  useEffect(() => {
    refresh();
    api.GET("/api/v1/boats" as any, {} as any).then(({ data }: any) => setBoats((data as Boat[]) ?? []));
  }, []);

  function toggleFilter(f: VisibilityFilter) {
    setActiveFilters((prev) => {
      const next = new Set(prev);
      if (next.has(f)) next.delete(f); else next.add(f);
      return next;
    });
    setPage(1);
  }

  const filtered = useMemo(() => {
    if (!sessions) return [];
    return sessions.filter((s) => {
      // Visibility filter (additive — show if matches any active tab)
      const matchesMine = activeFilters.has("mine") && s.isOwned;
      const matchesTeam = activeFilters.has("team") && !s.isOwned && s.sharedViaTeams && s.sharedViaTeams.length > 0;
      const matchesPublic = activeFilters.has("public") && s.isPublic;
      if (!matchesMine && !matchesTeam && !matchesPublic) return false;

      if (search && !s.fileName.toLowerCase().includes(search.toLowerCase())) return false;
      if (boatFilter && String(s.boatId ?? "") !== boatFilter) return false;
      if (dateFrom && new Date(s.startedAt) < new Date(dateFrom)) return false;
      if (dateTo && new Date(s.startedAt) > new Date(dateTo + "T23:59:59")) return false;
      return true;
    });
  }, [sessions, search, boatFilter, dateFrom, dateTo, activeFilters]);

  const sorted = useMemo(() => {
    if (!sortField || !sortDir) return filtered;
    const dir = sortDir === "asc" ? 1 : -1;
    return [...filtered].sort((a, b) => {
      const av = (a as any)[sortField];
      const bv = (b as any)[sortField];
      if (av == null) return 1;
      if (bv == null) return -1;
      if (typeof av === "number" || typeof bv === "number") return (Number(av) - Number(bv)) * dir;
      return String(av).localeCompare(String(bv)) * dir;
    });
  }, [filtered, sortField, sortDir]);

  const paged = sorted.slice((page - 1) * pageSize, page * pageSize);

  const handleSort = (f: SortField) => {
    if (sortField === f) setSortDir(sortDir === "asc" ? "desc" : sortDir === "desc" ? null : "asc");
    else { setSortField(f); setSortDir("asc"); }
  };

  const doDelete = async (s: SessionSummary) => {
    const res = await fetch(`/api/v1/sessions/${s.id}`, { method: "DELETE" });
    setConfirmDelete(null);
    if (res.ok || res.status === 204) {
      toast.push({ kind: "success", message: `Deleted ${s.fileName}` });
      refresh();
    } else {
      toast.push({ kind: "error", message: "Failed to delete session" });
    }
  };

  if (error) return <ErrorBanner message={error} onRetry={refresh} />;
  if (sessions === null) {
    return <div className="space-y-2">{Array.from({ length: 5 }).map((_, i) => <SkeletonLoader key={i} className="h-12" />)}</div>;
  }
  if (sessions.length === 0) {
    return (
      <EmptyState title="No sessions yet" description="Upload a .vkx file to get started." actionLabel="Upload" actionHref="/upload" />
    );
  }

  const filterTabs: { id: VisibilityFilter; label: string }[] = [
    { id: "mine", label: "My Sessions" },
    { id: "team", label: "Team Sessions" },
    { id: "public", label: "Public" },
  ];

  return (
    <div>
      <h1 className="mb-4 text-2xl font-bold">Sessions</h1>

      <div className="mb-3 flex flex-wrap gap-1">
        {filterTabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => toggleFilter(tab.id)}
            className={cn(
              "rounded-full border px-3 py-1 text-sm transition",
              activeFilters.has(tab.id)
                ? "border-action-primary bg-action-primary/10 text-action-primary"
                : "border-border-default text-text-secondary hover:border-action-primary/50"
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <FilterToolbar>
        <SearchInput value={search} onChange={setSearch} placeholder="Search file name…" />
        <select value={boatFilter} onChange={(e) => setBoatFilter(e.target.value)} className="rounded-md border border-border-default bg-bg-base px-2 py-1.5 text-sm text-text-primary">
          <option value="">All boats</option>
          {boats.map((b) => <option key={String(b.id)} value={String(b.id)}>{b.name}</option>)}
        </select>
        <Input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} className="w-40" />
        <span className="text-text-secondary">→</span>
        <Input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} className="w-40" />
      </FilterToolbar>

      <Card className="mt-4 overflow-hidden">
        <table className="w-full">
          <thead className="bg-bg-elevated">
            <tr>
              <SortableTableHeader<SortField> label="File" field="fileName" sortField={sortField} sortDir={sortDir} onSort={handleSort} />
              <SortableTableHeader<SortField> label="Boat" field="boatName" sortField={sortField} sortDir={sortDir} onSort={handleSort} />
              <SortableTableHeader<SortField> label="Course" field="courseName" sortField={sortField} sortDir={sortDir} onSort={handleSort} />
              <SortableTableHeader<SortField> label="Started" field="startedAt" sortField={sortField} sortDir={sortDir} onSort={handleSort} />
              <SortableTableHeader<SortField> label="Duration" field="durationSeconds" sortField={sortField} sortDir={sortDir} onSort={handleSort} />
              <SortableTableHeader<SortField> label="Races" field="raceCount" sortField={sortField} sortDir={sortDir} onSort={handleSort} />
              <th className="w-10" />
            </tr>
          </thead>
          <tbody>
            {paged.map((s) => {
              const dur = (new Date(s.endedAt).getTime() - new Date(s.startedAt).getTime()) / 1000;
              return (
                <tr key={String(s.id)} className="cursor-pointer border-t border-border-default text-sm hover:bg-bg-elevated/40">
                  <td className="px-3 py-2">
                    <Link href={`/sessions/${s.id}`}>{s.fileName}</Link>
                    <SessionBadges session={s} />
                  </td>
                  <td className="px-3 py-2 text-text-secondary">{s.boatName ?? "—"}</td>
                  <td className="px-3 py-2 text-text-secondary">{s.courseName ?? "—"}</td>
                  <td className="px-3 py-2 text-text-secondary">{new Date(s.startedAt).toLocaleString()}</td>
                  <td className="px-3 py-2 font-mono">{formatDuration(dur)}</td>
                  <td className="px-3 py-2">{n(s.raceCount)}</td>
                  <td className="px-3 py-2">
                    <ThreeDotMenu items={[
                      { label: "Open", onClick: () => location.assign(`/sessions/${s.id}`) },
                      ...(s.isOwned ? [{ label: "Delete", destructive: true, onClick: () => setConfirmDelete(s) }] : []),
                    ]} />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        <PaginationControls
          page={page}
          pageSize={pageSize}
          total={sorted.length}
          onPageChange={setPage}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1); }}
        />
      </Card>

      <ConfirmDialog
        open={!!confirmDelete}
        title="Delete session"
        message={`Delete "${confirmDelete?.fileName}"? This cannot be undone.`}
        destructive
        confirmLabel="Delete"
        onConfirm={() => confirmDelete && doDelete(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />
    </div>
  );
}
