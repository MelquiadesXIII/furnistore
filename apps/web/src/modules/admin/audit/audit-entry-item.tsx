import { formatMoment } from "@/lib/format-date";
import { actorLabel, fieldLabel, valueLabel } from "@/modules/admin/audit/audit-labels";
import type { AuditEntry } from "@/modules/admin/types";

export function AuditEntryItem({ entry, showChanges = true }: { entry: AuditEntry; showChanges?: boolean }) {
  const changes = Object.entries(entry.changes);

  return (
    <li className="flex flex-col gap-1 py-3 first:pt-0 last:pb-0">
      <span className="text-sm text-ink">{entry.summary}</span>
      <span className="text-xs text-ink-muted">
        {formatMoment(entry.occurredAt)} · {actorLabel(entry.actor)}
      </span>
      {showChanges && changes.length > 0 && (
        <dl className="mt-1 grid grid-cols-[auto_1fr] gap-x-3 gap-y-0.5 text-xs">
          {changes.map(([field, change]) => (
            <div key={field} className="contents">
              <dt className="text-ink-muted">{fieldLabel(field)}</dt>
              <dd className="break-words text-ink">
                {change.from !== null && (
                  <>
                    <span className="text-ink-muted line-through">{valueLabel(change.from)}</span>{" "}
                    →{" "}
                  </>
                )}
                {valueLabel(change.to)}
              </dd>
            </div>
          ))}
        </dl>
      )}
    </li>
  );
}
