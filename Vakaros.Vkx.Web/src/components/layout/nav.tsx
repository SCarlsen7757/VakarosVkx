"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { NAV_ITEMS, isActive, type NavItem } from "./nav-items";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/cn";
import { ThemeToggle } from "./theme-toggle";
import { useAuth } from "@/lib/auth-context";

function useVisibleNavItems(): NavItem[] {
  const { me, providers } = useAuth();
  const isAdmin = providers?.mode === "SingleUser" || !!me?.roles?.includes("Admin");
  return NAV_ITEMS.filter((i) => !i.adminOnly || isAdmin);
}

export function Sidebar() {
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);
  const items = useVisibleNavItems();

  return (
    <aside
      className={cn(
        "hidden xl:flex flex-col border-r border-border-default bg-bg-surface transition-all",
        collapsed ? "w-16" : "w-56"
      )}
    >
      <div className={cn("flex items-center px-3 py-4", collapsed ? "justify-center" : "justify-between")}>
        {!collapsed && (
          <span className="text-lg font-bold text-action-primary">Vakaros VKX</span>
        )}
        <button
          onClick={() => setCollapsed((v) => !v)}
          className="rounded p-1 text-text-secondary hover:bg-bg-elevated hover:text-text-primary"
          aria-label="Toggle sidebar"
        >
          {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
        </button>
      </div>
      <nav className="flex-1 space-y-1 px-2">
        {items.map((item) => {
          const active = isActive(pathname, item);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition",
                active
                  ? "bg-action-primary/10 text-action-primary"
                  : "text-text-secondary hover:bg-bg-elevated hover:text-text-primary",
                collapsed && "justify-center"
              )}
              title={collapsed ? item.label : undefined}
            >
              <item.icon className="h-5 w-5" />
              {!collapsed && <span>{item.label}</span>}
            </Link>
          );
        })}
      </nav>
      <div className={cn("px-2 py-3", collapsed && "flex justify-center")}>
        <ThemeToggle />
      </div>
    </aside>
  );
}

export function IconRail() {
  const pathname = usePathname();
  const items = useVisibleNavItems();
  return (
    <aside className="hidden lg:flex xl:hidden w-16 flex-col items-center border-r border-border-default bg-bg-surface py-4">
      <span className="mb-4 text-xs font-bold text-action-primary">VKX</span>
      <nav className="flex-1 space-y-1">
        {items.map((item) => {
          const active = isActive(pathname, item);
          return (
            <Link
              key={item.href}
              href={item.href}
              title={item.label}
              className={cn(
                "flex items-center justify-center rounded-md p-2",
                active ? "bg-action-primary/10 text-action-primary" : "text-text-secondary hover:bg-bg-elevated hover:text-text-primary"
              )}
            >
              <item.icon className="h-5 w-5" />
            </Link>
          );
        })}
      </nav>
      <ThemeToggle />
    </aside>
  );
}

export function BottomTabBar() {
  const pathname = usePathname();
  const items = useVisibleNavItems();
  return (
    <nav className="lg:hidden fixed bottom-0 inset-x-0 z-30 border-t border-border-default bg-bg-surface">
      <ul className={cn("grid", items.length <= 8 ? "grid-cols-8" : "grid-cols-9")}>
        {items.map((item) => {
          const active = isActive(pathname, item);
          return (
            <li key={item.href}>
              <Link
                href={item.href}
                className={cn(
                  "flex flex-col items-center justify-center gap-0.5 py-2 text-[10px]",
                  active ? "text-action-primary" : "text-text-secondary"
                )}
              >
                <item.icon className="h-5 w-5" />
                <span>{item.label}</span>
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
