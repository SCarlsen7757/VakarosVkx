import { Sidebar, IconRail, BottomTabBar } from "@/components/layout/nav";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <IconRail />
      <main className="flex-1 overflow-x-hidden pb-16 lg:pb-0">
        <div className="mx-auto max-w-7xl p-4 sm:p-6">{children}</div>
      </main>
      <BottomTabBar />
    </div>
  );
}
