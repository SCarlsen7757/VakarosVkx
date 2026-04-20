"use client";

import {
  Home, Upload, Layers, Ship, MapPin, Settings as SettingsIcon, type LucideIcon,
} from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  matchPrefix?: string;
}

export const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Home", icon: Home, matchPrefix: "/" },
  { href: "/upload", label: "Upload", icon: Upload, matchPrefix: "/upload" },
  { href: "/sessions", label: "Sessions", icon: Layers, matchPrefix: "/sessions" },
  { href: "/boats", label: "Fleet", icon: Ship, matchPrefix: "/boat" },
  { href: "/courses", label: "Courses", icon: MapPin, matchPrefix: "/course" },
  { href: "/settings", label: "Settings", icon: SettingsIcon, matchPrefix: "/settings" },
];

export function isActive(pathname: string, item: NavItem): boolean {
  if (item.href === "/") return pathname === "/";
  return pathname === item.href || pathname.startsWith(item.matchPrefix ?? item.href);
}
