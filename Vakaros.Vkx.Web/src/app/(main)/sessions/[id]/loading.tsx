export default function SessionDetailLoading() {
  return (
    <div className="shimmer-container space-y-6">
      {/* Breadcrumb */}
      <div className="shimmer h-5 w-56 rounded" />
      {/* Metadata card */}
      <div className="shimmer h-48 rounded-lg" />
      {/* Races card */}
      <div className="shimmer h-36 rounded-lg" />
      {/* Sharing card */}
      <div className="shimmer h-24 rounded-lg" />
    </div>
  );
}
