"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import type { Boat, BoatClass } from "@/lib/schemas";
import { Button, Card, Input, Select } from "@/components/ui/controls";
import { ErrorBanner } from "@/components/ui/error-banner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { SkeletonLoader } from "@/components/ui/skeleton-loader";
import { FilterToolbar, SearchInput } from "@/components/ui/filter-toolbar";
import { ThreeDotMenu } from "@/components/ui/three-dot-menu";
import { useToast } from "@/hooks/useToast";
import { Pencil, Save, X, Plus } from "lucide-react";

export default function BoatsPage() {
  const toast = useToast();
  const [boats, setBoats] = useState<Boat[] | null>(null);
  const [classes, setClasses] = useState<BoatClass[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [classFilter, setClassFilter] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState<{ name: string; sailNumber: string; boatClassId: string; description: string }>({ name: "", sailNumber: "", boatClassId: "", description: "" });
  const [adding, setAdding] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState<Boat | null>(null);

  const load = () => {
    api.GET("/api/v1/boats" as any, {} as any).then(({ data, error }: any) => {
      if (error) setError("Failed to load boats");
      else setBoats(data as Boat[]);
    });
  };

  useEffect(() => {
    load();
    api.GET("/api/v1/boat-classes" as any, {} as any).then(({ data }: any) => setClasses((data as BoatClass[]) ?? []));
  }, []);

  const filtered = useMemo(() => {
    if (!boats) return [];
    return boats.filter((b) => {
      if (classFilter && String(b.boatClass.id) !== classFilter) return false;
      const q = search.toLowerCase();
      if (q && !b.name.toLowerCase().includes(q) && !(b.sailNumber ?? "").toLowerCase().includes(q)) return false;
      return true;
    });
  }, [boats, search, classFilter]);

  const startEdit = (b: Boat) => {
    setEditingId(String(b.id));
    setDraft({
      name: b.name,
      sailNumber: b.sailNumber ?? "",
      boatClassId: String(b.boatClass.id),
      description: b.description ?? "",
    });
  };
  const cancelEdit = () => { setEditingId(null); setAdding(false); };

  const submit = async (id?: string) => {
    if (!draft.name || !draft.boatClassId) { toast.push({ kind: "warning", message: "Name and class are required." }); return; }
    const body = {
      name: draft.name,
      sailNumber: draft.sailNumber || null,
      boatClassId: draft.boatClassId,
      description: draft.description || null,
    };
    const url = id ? `/api/v1/boats/${id}` : "/api/v1/boats";
    const method = id ? "PUT" : "POST";
    const res = await fetch(url, { method, headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    if (res.ok) {
      toast.push({ kind: "success", message: id ? "Boat updated." : "Boat created." });
      cancelEdit(); load();
    } else toast.push({ kind: "error", message: "Save failed." });
  };

  const doDelete = async (b: Boat) => {
    const res = await fetch(`/api/v1/boats/${b.id}`, { method: "DELETE" });
    setConfirmDelete(null);
    if (res.ok || res.status === 204) { toast.push({ kind: "success", message: "Boat deleted." }); load(); }
    else if (res.status === 409) toast.push({ kind: "error", message: "Cannot delete: boat is referenced by sessions." });
    else toast.push({ kind: "error", message: "Delete failed." });
  };

  if (error) return <ErrorBanner message={error} onRetry={load} />;
  if (!boats) return <div className="space-y-2">{Array.from({ length: 5 }).map((_, i) => <SkeletonLoader key={i} className="h-12" />)}</div>;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold">Fleet</h1>
        <Button onClick={() => { setAdding(true); setDraft({ name: "", sailNumber: "", boatClassId: classes[0] ? String(classes[0].id) : "", description: "" }); }}>
          <Plus className="h-4 w-4" /> New boat
        </Button>
      </div>
      <FilterToolbar>
        <SearchInput value={search} onChange={setSearch} placeholder="Name or sail #…" />
        <Select value={classFilter} onChange={(e) => setClassFilter(e.target.value)} className="w-48">
          <option value="">All classes</option>
          {classes.map((c) => <option key={String(c.id)} value={String(c.id)}>{c.name}</option>)}
        </Select>
        <Link href="/boat-classes" className="ml-auto text-sm text-action-primary hover:underline">Manage boat classes →</Link>
      </FilterToolbar>

      <Card className="mt-4 overflow-hidden">
        <table className="w-full">
          <thead className="bg-bg-elevated text-xs uppercase tracking-wider text-text-secondary">
            <tr>
              <th className="px-3 py-2 text-left">Name</th>
              <th className="px-3 py-2 text-left">Sail #</th>
              <th className="px-3 py-2 text-left">Class</th>
              <th className="px-3 py-2 text-left">Description</th>
              <th className="px-3 py-2 w-32"></th>
            </tr>
          </thead>
          <tbody>
            {adding && (
              <tr className="border-t border-border-default text-sm bg-bg-elevated/40">
                <td className="px-3 py-2"><Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} /></td>
                <td className="px-3 py-2"><Input value={draft.sailNumber} onChange={(e) => setDraft({ ...draft, sailNumber: e.target.value })} /></td>
                <td className="px-3 py-2">
                  <Select value={draft.boatClassId} onChange={(e) => setDraft({ ...draft, boatClassId: e.target.value })}>
                    {classes.map((c) => <option key={String(c.id)} value={String(c.id)}>{c.name}</option>)}
                  </Select>
                </td>
                <td className="px-3 py-2"><Input value={draft.description} onChange={(e) => setDraft({ ...draft, description: e.target.value })} /></td>
                <td className="px-3 py-2 flex gap-1">
                  <Button onClick={() => submit()}><Save className="h-4 w-4" /></Button>
                  <Button variant="ghost" onClick={cancelEdit}><X className="h-4 w-4" /></Button>
                </td>
              </tr>
            )}
            {filtered.map((b) => {
              const editing = editingId === String(b.id);
              return (
                <tr key={String(b.id)} className="border-t border-border-default text-sm">
                  <td className="px-3 py-2">{editing ? <Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} /> :
                    <Link className="text-action-primary hover:underline" href={`/boats/${b.id}`}>{b.name}</Link>}</td>
                  <td className="px-3 py-2">{editing ? <Input value={draft.sailNumber} onChange={(e) => setDraft({ ...draft, sailNumber: e.target.value })} /> : (b.sailNumber ?? "—")}</td>
                  <td className="px-3 py-2">
                    {editing ? (
                      <Select value={draft.boatClassId} onChange={(e) => setDraft({ ...draft, boatClassId: e.target.value })}>
                        {classes.map((c) => <option key={String(c.id)} value={String(c.id)}>{c.name}</option>)}
                      </Select>
                    ) : b.boatClass.name}
                  </td>
                  <td className="px-3 py-2 text-text-secondary">{editing ? <Input value={draft.description} onChange={(e) => setDraft({ ...draft, description: e.target.value })} /> : (b.description ?? "—")}</td>
                  <td className="px-3 py-2">
                    {editing ? (
                      <div className="flex gap-1">
                        <Button onClick={() => submit(String(b.id))}><Save className="h-4 w-4" /></Button>
                        <Button variant="ghost" onClick={cancelEdit}><X className="h-4 w-4" /></Button>
                      </div>
                    ) : (
                      <ThreeDotMenu items={[
                        { label: "Edit", onClick: () => startEdit(b) },
                        { label: "Delete", destructive: true, onClick: () => setConfirmDelete(b) },
                      ]} />
                    )}
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && !adding && (
              <tr><td colSpan={5} className="px-3 py-8 text-center text-text-secondary">No boats yet.</td></tr>
            )}
          </tbody>
        </table>
      </Card>

      <ConfirmDialog
        open={!!confirmDelete}
        title="Delete boat"
        message={`Delete "${confirmDelete?.name}"?`}
        destructive
        confirmLabel="Delete"
        onConfirm={() => confirmDelete && doDelete(confirmDelete)}
        onCancel={() => setConfirmDelete(null)}
      />
    </div>
  );
}
