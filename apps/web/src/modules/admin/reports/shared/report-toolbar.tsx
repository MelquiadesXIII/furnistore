"use client";

import { parseAsString, parseAsStringLiteral, useQueryStates } from "nuqs";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { REPORT_GROUPINGS, REPORTS, type ReportSlug } from "@/modules/admin/reports/definitions";
import { GROUPING_LABELS } from "@/modules/admin/reports/format";
import { datePresets, storeToday } from "@/modules/admin/reports/shared/date-presets";
import { ExportMenu } from "@/modules/admin/reports/shared/export-menu";
import type { ReportGrouping } from "@/modules/admin/reports/types";
import { useTableTransition } from "@/modules/admin/table/table-frame";

const CUSTOM = "custom";

export function ReportToolbar({
  slug,
  period,
  groupBy,
  availableGroupings,
  table,
}: {
  slug: ReportSlug;
  period: { from: string; to: string } | null;
  groupBy: ReportGrouping | null;
  availableGroupings: ReportGrouping[];
  table: { id: string; title: string } | null;
}) {
  const definition = REPORTS[slug];
  const startTransition = useTableTransition();
  const [params, setParams] = useQueryStates(
    {
      from: parseAsString,
      to: parseAsString,
      groupBy: parseAsStringLiteral(REPORT_GROUPINGS),
    },
    { shallow: false, startTransition },
  );

  const from = params.from ?? period?.from ?? "";
  const to = params.to ?? period?.to ?? "";
  const presets = datePresets(storeToday());
  const activePreset = presets.find((preset) => preset.from === from && preset.to === to);

  function applyRange(next: { from: string; to: string }) {
    if (!next.from || !next.to) return;
    setParams({ from: next.from, to: next.to, groupBy: null });
  }

  return (
    <div className="flex flex-wrap items-end justify-between gap-4 rounded-sm border border-hairline bg-surface-raised p-4">
      {definition.usesPeriod ? (
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="report-preset" className="text-xs text-ink-muted">
              Período
            </Label>
            <Select
              value={activePreset?.id ?? CUSTOM}
              onValueChange={(id) => {
                const preset = presets.find((candidate) => candidate.id === id);
                if (preset) applyRange(preset);
              }}
            >
              <SelectTrigger id="report-preset" className="w-44 bg-surface">
                <SelectValue>{activePreset?.label ?? "Personalizado"}</SelectValue>
              </SelectTrigger>
              <SelectContent>
                {presets.map((preset) => (
                  <SelectItem key={preset.id} value={preset.id}>
                    {preset.label}
                  </SelectItem>
                ))}
                <SelectItem value={CUSTOM} disabled>
                  Personalizado
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="report-from" className="text-xs text-ink-muted">
              Desde
            </Label>
            <Input
              id="report-from"
              type="date"
              value={from}
              max={to || undefined}
              onChange={(event) => applyRange({ from: event.target.value, to })}
              className="w-40 bg-surface"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="report-to" className="text-xs text-ink-muted">
              Hasta
            </Label>
            <Input
              id="report-to"
              type="date"
              value={to}
              min={from || undefined}
              onChange={(event) => applyRange({ from, to: event.target.value })}
              className="w-40 bg-surface"
            />
          </div>

          {definition.hasGrouping && groupBy && availableGroupings.length > 1 && (
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="report-grouping" className="text-xs text-ink-muted">
                Agrupar
              </Label>
              <Select
                value={groupBy}
                onValueChange={(next) => setParams({ groupBy: next as ReportGrouping })}
              >
                <SelectTrigger id="report-grouping" className="w-36 bg-surface">
                  <SelectValue>{GROUPING_LABELS[groupBy]}</SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {availableGroupings.map((grouping) => (
                    <SelectItem key={grouping} value={grouping}>
                      {GROUPING_LABELS[grouping]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}
        </div>
      ) : (
        <p className="text-sm text-ink-muted">Este reporte muestra el stock de hoy, no depende de fechas.</p>
      )}

      <ExportMenu slug={slug} period={period} groupBy={definition.hasGrouping ? groupBy : null} table={table} />
    </div>
  );
}
