import { cn } from "@/lib/cn";

export function SkeletonLoader({ className }: { className?: string }) {
  return <div className={cn("animate-pulse rounded bg-border-default/40", className)} />;
}
