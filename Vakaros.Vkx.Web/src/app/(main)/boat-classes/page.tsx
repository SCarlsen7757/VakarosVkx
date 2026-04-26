"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { BoatClass } from "@/lib/schemas";
import { Button, Card, Input } from "@/components/ui/controls";
import { ErrorBanner } from "@/components/ui/error-banner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { ThreeDotMenu } from "@/components/ui/three-dot-menu";
import { useToast } from "@/hooks/useToast";
import { Plus, X } from "lucide-react";

interface Draft {
  name: string;
  length: string;
  width: string;
  weight: string;
}

const emptyDraft: Draft = { name: "", length: "", width: "", weight: "" };

export default function BoatClassesPage() {
  const toast = useToast();
  const [list, setList] = useState<BoatClass[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [confirmDelete, setConfirmDelete] = useState<BoatClass | null>(null);

  const load = () => {
    api.GET("/api/v1/boat-classes" as any, {} as any).then(({ data, error }: any) => {
      if (error) setError("Failed to load");
      else setList(data as BoatClass[]);
    });
  };
  useEffect(load, []);

  const startEdit = (c: BoatClass | null) => {
    if (c) {
      setEditingId(String(c.id));
      setDraft({
        name: c.name,
        length: c.length == null ? "" : String(c.length),
        width: c.width == null ? "" : String(c.width),
        weight: c.weight == null ? "" : String(c.weight),
      });
    } else {
      setEditingId("new");
      setDraft(emptyDraft);
    }
  };

  const save = async () => {
    if (!draft.name) { toast.push({ kind: "warning", message: "Name required." }); return; }
    const body = {
      name: draft.name,
      length: draft.length ? Number(draft.length) : null,
      width: draft.width ? Number(draft.width) : null,
      weight: draft.weight ? Number(draft.weight) : null,
    };
    const isNew = editingId === "new";
    const url = isNew ? "/api/v1/boat-classes" : `/api/v1/boat-classes/${editingId}`;
    const res = await fetch(url, { method: isNew ? "POST" : "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    if (res.ok) { toast.push({ kind: "success", message: isNew ? "Created." : "Saved." }); setEditingId(null); load(); }
    else toast.push({ kind: "error", message: "Save failed." });
  };

  const doDelete = async (c: BoatClass) => {
    const res = await fetch(`/api/v1/boat-classes/${c.id}`, { method: "DELETE" });
    setConfirmDelete(null);
    if (res.ok || res.status === 204) { toast.push({ kind: "success", message: "Deleted." }); load(); }
    else if (res.status === 409) toast.push({ kind: "error", message: "Class is referenced by boats." });
    else toast.push({ kind: "error", message: "Delete failed." });
  };

  if (error) return <ErrorBanner message={error} onRetry={load} />;
  if (!list) return <SkeletonLoader className="h-40" />;

  return (
    <div className={editingId ? "grid gap-6 lg:grid-cols-[1fr_24rem]" : undefined}>
      <div>
        <div className="mb-4 flex items-center justify-between">
          <h1 className="text-2xl font-bold">Boat classes</h1>
          <Button onClick={() => startEdit(null)}><Plus className="h-4 w-4" /> New class</Button>
        </div>
        <Card className="overflow-hidden">
          <table className="w-full">
            <thead className="bg-bg-elevated text-xs uppercase tracking-wider text-text-secondary">
              <tr>
                <th className="px-3 py-2 text-left">Name</th>
                <th className="px-3 py-2 text-left">Length (m)</th>
                <th className="px-3 py-2 text-left">Width (m)</th>
                <th className="px-3 py-2 text-left">Weight (kg)</th>
                <th className="w-10"></th>
              </tr>
            </thead>
            <tbody>
              {list.map((c) => (
                <tr key={String(c.id)} className="cursor-pointer border-t border-border-default text-sm hover:bg-bg-elevated/40" onClick={() => startEdit(c)}>
                  <td className="px-3 py-2">{c.name}</td>
                  <td className="px-3 py-2 text-text-secondary">{c.length ?? "—"}</td>
                  <td className="px-3 py-2 text-text-secondary">{c.width ?? "—"}</td>
                  <td className="px-3 py-2 text-text-secondary">{c.weight ?? "—"}</td>
                  <td className="px-3 py-2">
                    <ThreeDotMenu items={[
                      { label: "Edit", onClick: () => startEdit(c) },
                      { label: "Delete", destructive: true, onClick: () => setConfirmDelete(c) },
                    ]} />
                  </td>
                </tr>
              ))}
              {list.length === 0 && <tr><td colSpan={5} className="px-3 py-8 text-center text-text-secondary">No boat classes yet.</td></tr>}
            </tbody>
          </table>
        </Card>
      </div>

      {editingId && (
        <Card className="p-5 self-start">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">{editingId === "new" ? "New class" : "Edit class"}</h2>
            <button className="text-text-secondary hover:text-text-primary" onClick={() => setEditingId(null)}><X className="h-4 w-4" /></button>
          </div>
          <div className="space-y-3">
            <label className="block"><span className="text-sm text-text-secondary">Name</span><Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} /></label>
            <div className="grid grid-cols-2 gap-3">
              <label className="min-w-0"><span className="text-sm text-text-secondary">Length (m)</span><Input type="number" step="0.01" value={draft.length} onChange={(e) => setDraft({ ...draft, length: e.target.value })} /></label>
              <label className="min-w-0"><span className="text-sm text-text-secondary">Width (m)</span><Input type="number" step="0.01" value={draft.width} onChange={(e) => setDraft({ ...draft, width: e.target.value })} /></label>
              <label className="min-w-0"><span className="text-sm text-text-secondary">Weight (kg)</span><Input type="number" value={draft.weight} onChange={(e) => setDraft({ ...draft, weight: e.target.value })} /></label>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" onClick={() => setEditingId(null)}>Cancel</Button>
              <Button onClick={save}>Save</Button>
            </div>
          </div>
        </Card>
      )}

      <ConfirmDialog
        open={!!confirmDelete}
        title="Delete boat class"
        message={`Delete "${confirmDelete?.name}"?`}
        destructive
        confirmLabel="Delete"
        onConfirm={() => confirmDelete && doDelete(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />
    </div>
  );
}
