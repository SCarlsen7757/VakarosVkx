"use client";

import { useEffect, useRef, useState } from "react";
import { MoreVertical } from "lucide-react";

export interface ThreeDotItem {
  label: string;
  onClick: () => void;
  destructive?: boolean;
}

export function ThreeDotMenu({ items }: { items: ThreeDotItem[] }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onClick = (e: MouseEvent) => {
      if (!ref.current?.contains(e.target as Node)) setOpen(false);
    };
    window.addEventListener("mousedown", onClick);
    return () => window.removeEventListener("mousedown", onClick);
  }, [open]);

  return (
    <div ref={ref} className="relative inline-block">
      <button
        onClick={(e) => { e.stopPropagation(); setOpen((v) => !v); }}
        className="rounded p-1 text-text-secondary hover:bg-bg-elevated hover:text-text-primary"
        aria-label="More actions"
      >
        <MoreVertical className="h-4 w-4" />
      </button>
      {open && (
        <div className="absolute right-0 z-30 mt-1 min-w-[10rem] overflow-hidden rounded-md bg-bg-elevated shadow-lg ring-1 ring-border-default">
          {items.map((it) => (
            <button
              key={it.label}
              onClick={(e) => { e.stopPropagation(); setOpen(false); it.onClick(); }}
              className={`block w-full px-3 py-2 text-left text-sm hover:bg-bg-base ${
                it.destructive ? "text-error" : "text-text-primary"
              }`}
            >
              {it.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
