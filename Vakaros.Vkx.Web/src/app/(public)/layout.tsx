"use client";

import Link from "next/link";
import { Sidebar, IconRail, BottomTabBar } from "@/components/layout/nav";
import { ModeBanner } from "@/components/layout/auth-gate";
import { useAuth } from "@/lib/auth-context";

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  const { me, providers, loading } = useAuth();
  const showNav = !!me || providers?.mode === "SingleUser";

  return (
    <div className="flex min-h-screen flex-col">
      <ModeBanner />
      {!loading && !showNav && (
        <div className="flex items-center justify-between border-b border-border-default bg-bg-elevated px-4 py-2 text-sm">
          <Link href="/sessions" className="font-semibold">Vakaros VKX</Link>
          <div className="flex items-center gap-4">
            <Link href="/sessions" className="text-text-secondary hover:text-text-primary">Sessions</Link>
            <Link href="/boat-classes" className="text-text-secondary hover:text-text-primary">Boat classes</Link>
            <Link href="/login" className="rounded-md bg-action-primary px-3 py-1 text-white hover:opacity-90">Sign in</Link>
          </div>
        </div>
      )}
      <div className="flex flex-1">
        {showNav && <Sidebar />}
        {showNav && <IconRail />}
        <main className="flex-1 overflow-x-hidden pb-16 lg:pb-0">
          <div className="p-4 sm:p-6">{children}</div>
        </main>
        {showNav && <BottomTabBar />}
      </div>
    </div>
  );
}
