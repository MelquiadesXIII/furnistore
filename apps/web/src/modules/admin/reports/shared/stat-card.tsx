export function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-sm border border-hairline bg-surface-raised p-4">
      <span className="text-xs text-ink-muted uppercase">{label}</span>
      <span className="font-mono text-2xl text-ink">{value}</span>
      {hint && <span className="text-xs text-ink-muted">{hint}</span>}
    </div>
  );
}
