export default function RaceViewerLoading() {
  return (
    <div className="shimmer-container flex flex-col gap-3">
      {/* Header bar */}
      <div className="shimmer h-7 w-48 rounded" />
      {/* Two-column body */}
      <div className="flex flex-col gap-4 lg:flex-row" style={{ minHeight: "calc(100vh - 8rem)" }}>
        {/* Left: map area */}
        <div className="flex flex-col gap-3 lg:w-[42%]">
          <div className="shimmer h-10 rounded" />
          <div className="shimmer flex-1 rounded" style={{ minHeight: "20rem" }} />
        </div>
        {/* Right: detail panel */}
        <div className="flex flex-1 flex-col gap-4">
          <div className="shimmer h-28 rounded" />
          <div className="shimmer h-40 rounded" />
          <div className="shimmer h-48 rounded" />
        </div>
      </div>
    </div>
  );
}
