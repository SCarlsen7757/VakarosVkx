"use client";

import { useEffect, useMemo, useState } from "react";
import { api } from "@/lib/api";
import type { Mark } from "@/lib/schemas";
import { Button, Card, Input } from "@/components/ui/controls";
import { ErrorBanner } from "@/components/ui/error-banner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { FilterToolbar, SearchInput } from "@/components/ui/filter-toolbar";
import { ThreeDotMenu } from "@/components/ui/three-dot-menu";
import { useToast } from "@/hooks/useToast";
import { Plus, Save, X } from "lucide-react";

interface Draft { name: string; activeFrom: string; activeUntil: string; latitude: string; longitude: string; description: string; }
const emptyDraft: Draft = { name: "", activeFrom: new Date().toISOString().slice(0, 10), activeUntil: "", latitude: "", longitude: "", description: "" };

export default function MarksPage() {
  const toast = useToast();
  const [list, setList] = useState<Mark[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [activeOnDate, setActiveOnDate] = useState("");
  const [activeOnly, setActiveOnly] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [adding, setAdding] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState<Mark | null>(null);

  const load = () => api.GET("/api/v1/marks" as any, {} as any).then(({ data, error }: any) => {
    if (error) setError("Failed to load"); else setList(data as Mark[]);
  });
  useEffect(() => { load(); }, []);

  const filtered = useMemo(() => {
    if (!list) return [];
    const today = new Date().toISOString().slice(0, 10);
    return list.filter((m) => {
      const q = search.toLowerCase();
      if (q && !m.name.toLowerCase().includes(q)) return false;
      if (activeOnly) {
        if (m.activeUntil && m.activeUntil < today) return false;
      }
      if (activeOnDate) {
        if (m.activeFrom > activeOnDate) return false;
        if (m.activeUntil && m.activeUntil < activeOnDate) return false;
      }
      return true;
    });
  }, [list, search, activeOnDate, activeOnly]);

  const startEdit = (m: Mark) => {
    setEditingId(String(m.id));
    setDraft({
      name: m.name,
      activeFrom: m.activeFrom,
      activeUntil: m.activeUntil ?? "",
      latitude: String(m.latitude),
      longitude: String(m.longitude),
      description: m.description ?? "",
    });
  };

  const submit = async (id?: string) => {
    if (!draft.name || !draft.latitude || !draft.longitude) { toast.push({ kind: "warning", message: "Name, lat, lon required." }); return; }
    const body = {
      name: draft.name,
      activeFrom: draft.activeFrom,
      activeUntil: draft.activeUntil || null,
      latitude: Number(draft.latitude),
      longitude: Number(draft.longitude),
      description: draft.description || null,
    };
    const url = id ? `/api/v1/marks/${id}` : "/api/v1/marks";
    const res = await fetch(url, { method: id ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    if (res.ok) { toast.push({ kind: "success", message: id ? "Mark saved." : "Mark created." }); setEditingId(null); setAdding(false); load(); }
    else toast.push({ kind: "error", message: "Save failed." });
  };

  const doDelete = async (m: Mark) => {
    const res = await fetch(`/api/v1/marks/${m.id}`, { method: "DELETE" });
    setConfirmDelete(null);
    if (res.ok || res.status === 204) { toast.push({ kind: "success", message: "Mark deleted." }); load(); }
    else if (res.status === 409) toast.push({ kind: "error", message: "Mark is used in a course." });
    else toast.push({ kind: "error", message: "Delete failed." });
  };

  if (error) return <ErrorBanner message={error} onRetry={load} />;
  if (!list) return <SkeletonLoader className="h-40" />;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold">Marks</h1>
        <Button onClick={() => { setAdding(true); setDraft(emptyDraft); }}><Plus className="h-4 w-4" /> New mark</Button>
      </div>
      <FilterToolbar>
        <SearchInput value={search} onChange={setSearch} placeholder="Name…" />
        <Input type="date" value={activeOnDate} onChange={(e) => setActiveOnDate(e.target.value)} className="w-44" />
        <label className="flex items-center gap-1 text-sm text-text-secondary">
          <input type="checkbox" checked={activeOnly} onChange={(e) => setActiveOnly(e.target.checked)} /> Active only
        </label>
      </FilterToolbar>

      <Card className="mt-4 overflow-hidden">
        <table className="w-full">
          <thead className="bg-bg-elevated text-xs uppercase tracking-wider text-text-secondary">
            <tr>
              <th className="px-3 py-2 text-left">Name</th>
              <th className="px-3 py-2 text-left">Active from</th>
              <th className="px-3 py-2 text-left">Active until</th>
              <th className="px-3 py-2 text-left">Lat</th>
              <th className="px-3 py-2 text-left">Lon</th>
              <th className="px-3 py-2 text-left">Description</th>
              <th className="w-32"></th>
            </tr>
          </thead>
          <tbody>
            {adding && (
              <tr className="border-t border-border-default text-sm bg-bg-elevated/40">
                <td className="px-2 py-2"><Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} /></td>
                <td className="px-2 py-2"><Input type="date" value={draft.activeFrom} onChange={(e) => setDraft({ ...draft, activeFrom: e.target.value })} /></td>
                <td className="px-2 py-2"><Input type="date" value={draft.activeUntil} onChange={(e) => setDraft({ ...draft, activeUntil: e.target.value })} /></td>
                <td className="px-2 py-2"><Input type="number" step="0.000001" value={draft.latitude} onChange={(e) => setDraft({ ...draft, latitude: e.target.value })} /></td>
                <td className="px-2 py-2"><Input type="number" step="0.000001" value={draft.longitude} onChange={(e) => setDraft({ ...draft, longitude: e.target.value })} /></td>
                <td className="px-2 py-2"><Input value={draft.description} onChange={(e) => setDraft({ ...draft, description: e.target.value })} /></td>
                <td className="px-2 py-2 flex gap-1"><Button onClick={() => submit()}><Save className="h-4 w-4" /></Button><Button variant="ghost" onClick={() => setAdding(false)}><X className="h-4 w-4" /></Button></td>
              </tr>
            )}
            {filtered.map((m) => {
              const editing = editingId === String(m.id);
              return (
                <tr key={String(m.id)} className="border-t border-border-default text-sm">
                  <td className="px-2 py-2">{editing ? <Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} /> : m.name}</td>
                  <td className="px-2 py-2">{editing ? <Input type="date" value={draft.activeFrom} onChange={(e) => setDraft({ ...draft, activeFrom: e.target.value })} /> : m.activeFrom}</td>
                  <td className="px-2 py-2">{editing ? <Input type="date" value={draft.activeUntil} onChange={(e) => setDraft({ ...draft, activeUntil: e.target.value })} /> : (m.activeUntil ?? "—")}</td>
                  <td className="px-2 py-2">{editing ? <Input type="number" step="0.000001" value={draft.latitude} onChange={(e) => setDraft({ ...draft, latitude: e.target.value })} /> : Number(m.latitude).toFixed(6)}</td>
                  <td className="px-2 py-2">{editing ? <Input type="number" step="0.000001" value={draft.longitude} onChange={(e) => setDraft({ ...draft, longitude: e.target.value })} /> : Number(m.longitude).toFixed(6)}</td>
                  <td className="px-2 py-2 text-text-secondary">{editing ? <Input value={draft.description} onChange={(e) => setDraft({ ...draft, description: e.target.value })} /> : (m.description ?? "—")}</td>
                  <td className="px-2 py-2">
                    {editing ? (
                      <div className="flex gap-1"><Button onClick={() => submit(String(m.id))}><Save className="h-4 w-4" /></Button><Button variant="ghost" onClick={() => setEditingId(null)}><X className="h-4 w-4" /></Button></div>
                    ) : (
                      <ThreeDotMenu items={[
                        { label: "Edit", onClick: () => startEdit(m) },
                        { label: "Delete", destructive: true, onClick: () => setConfirmDelete(m) },
                      ]} />
                    )}
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && !adding && <tr><td colSpan={7} className="px-3 py-8 text-center text-text-secondary">No marks.</td></tr>}
          </tbody>
        </table>
      </Card>

      <ConfirmDialog
        open={!!confirmDelete}
        title="Delete mark"
        message={`Delete "${confirmDelete?.name}"?`}
        destructive
        confirmLabel="Delete"
        onConfirm={() => confirmDelete && doDelete(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />
    </div>
  );
}
