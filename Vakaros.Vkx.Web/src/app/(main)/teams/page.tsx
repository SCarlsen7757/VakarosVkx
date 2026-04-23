"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import type { components } from "@/lib/api-types";

type Team = components["schemas"]["TeamDto"];

export default function TeamsPage() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [name, setName] = useState("");

  async function load() {
    const { data } = await api.GET("/api/v1/teams");
    setTeams(data ?? []);
  }

  useEffect(() => { void load(); }, []);

  async function createTeam(e: React.FormEvent) {
    e.preventDefault();
    if (!name) return;
    await api.POST("/api/v1/teams", { body: { name } });
    setName("");
    await load();
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-action-primary">Teams</h1>
      <form onSubmit={createTeam} className="flex gap-2">
        <input className="flex-1 rounded border border-border-default bg-bg-surface p-2" placeholder="New team name" value={name} onChange={(e) => setName(e.target.value)} />
        <button type="submit" disabled={!name} className="rounded bg-action-primary px-3 py-1.5 text-white disabled:opacity-50">Create</button>
      </form>
      <ul className="space-y-2">
        {teams.map((t) => (
          <li key={t.id} className="rounded border border-border-default p-3">
            <Link href={`/teams/${t.id}`} className="text-action-primary hover:underline">{t.name}</Link>
          </li>
        ))}
        {teams.length === 0 && <li className="text-sm text-text-secondary">You aren&apos;t a member of any teams yet.</li>}
      </ul>
    </div>
  );
}
