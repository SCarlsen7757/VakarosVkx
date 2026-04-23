"use client";

import { use, useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { components } from "@/lib/api-types";

type Member = components["schemas"]["TeamMemberDto"];

export default function TeamDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [members, setMembers] = useState<Member[]>([]);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteSent, setInviteSent] = useState(false);

  async function load() {
    const { data } = await api.GET("/api/v1/teams/{teamId}/members", { params: { path: { teamId: id } } });
    setMembers(data ?? []);
  }

  useEffect(() => { void load(); }, [id]);

  async function invite(e: React.FormEvent) {
    e.preventDefault();
    await api.POST("/api/v1/teams/{teamId}/invites", { params: { path: { teamId: id } }, body: { email: inviteEmail, role: "Member" } });
    setInviteEmail("");
    setInviteSent(true);
    setTimeout(() => setInviteSent(false), 3000);
  }

  async function removeMember(memberId: string) {
    if (!confirm("Remove member?")) return;
    await api.DELETE("/api/v1/teams/{teamId}/members/{memberId}", { params: { path: { teamId: id, memberId } } });
    await load();
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-action-primary">Team</h1>
      <section>
        <h2 className="mb-2 text-lg font-semibold">Invite member</h2>
        <form onSubmit={invite} className="flex gap-2">
          <input className="flex-1 rounded border border-border-default bg-bg-surface p-2" type="email" placeholder="Email" value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} required />
          <button type="submit" className="rounded bg-action-primary px-3 py-1.5 text-white">Send invite</button>
        </form>
        {inviteSent && <p className="mt-2 text-sm text-green-500">Invite sent.</p>}
      </section>
      <section>
        <h2 className="mb-2 text-lg font-semibold">Members</h2>
        <ul className="space-y-1">
          {members.map((m) => (
            <li key={m.userId} className="flex items-center justify-between rounded border border-border-default p-2 text-sm">
              <span>{m.displayName ?? m.email} <span className="text-text-secondary">({m.role})</span></span>
              <button onClick={() => removeMember(m.userId)} className="text-red-500 hover:underline">Remove</button>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}