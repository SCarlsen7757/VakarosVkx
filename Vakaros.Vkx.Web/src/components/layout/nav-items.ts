"use client";

import {
  Home, Upload, Layers, Sailboat, MapPin, Users, UserCircle, Settings as SettingsIcon, ShieldCheck, Inbox, type LucideIcon,
} from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  matchPrefix?: string;
  adminOnly?: boolean;
}

export const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Home", icon: Home, matchPrefix: "/" },
  { href: "/upload", label: "Upload", icon: Upload, matchPrefix: "/upload" },
  { href: "/sessions", label: "Sessions", icon: Layers, matchPrefix: "/sessions" },
  { href: "/boats", label: "Fleet", icon: Sailboat, matchPrefix: "/boat" },
  { href: "/courses", label: "Courses", icon: MapPin, matchPrefix: "/course" },
  { href: "/teams", label: "Teams", icon: Users, matchPrefix: "/teams" },
  { href: "/account", label: "Account", icon: UserCircle, matchPrefix: "/account" },
  { href: "/admin/users", label: "Admin", icon: ShieldCheck, matchPrefix: "/admin/users", adminOnly: true },
  { href: "/admin/boat-classes", label: "Class requests", icon: Inbox, matchPrefix: "/admin/boat-classes", adminOnly: true },
  { href: "/settings", label: "Settings", icon: SettingsIcon, matchPrefix: "/settings" },
];

export function isActive(pathname: string, item: NavItem): boolean {
  if (item.href === "/") return pathname === "/";
  return pathname === item.href || pathname.startsWith(item.matchPrefix ?? item.href);
}
