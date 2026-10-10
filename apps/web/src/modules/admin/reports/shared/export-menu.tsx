"use client";

import { Download, FileSpreadsheet, FileText, LoaderCircle } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { ReportSlug } from "@/modules/admin/reports/definitions";
import type { ReportGrouping } from "@/modules/admin/reports/types";

type ExportJob = { href: string; label: string; fallbackName: string };

const FILENAME_PATTERN = /filename="?([^";]+)"?/i;

function fileNameFrom(disposition: string | null): string | null {
  return disposition?.match(FILENAME_PATTERN)?.[1] ?? null;
}

function save(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = name;
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function ExportMenu({
  slug,
  period,
  groupBy,
  table,
}: {
  slug: ReportSlug;
  period: { from: string; to: string } | null;
  groupBy: ReportGrouping | null;
  table: { id: string; title: string } | null;
}) {
  const [pending, setPending] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  function href(target: string, format: "pdf" | "csv", tableId?: string): string {
    const params = new URLSearchParams();
    if (period) {
      params.set("from", period.from);
      params.set("to", period.to);
    }
    if (groupBy) params.set("groupBy", groupBy);
    if (tableId) params.set("table", tableId);
    return `/admin/reports/${target}/${format}?${params.toString()}`;
  }

  async function run(job: ExportJob) {
    setPending(job.label);
    setError(null);

    try {
      const response = await fetch(job.href, { cache: "no-store" });

      if (!response.ok) {
        const body = (await response.json().catch(() => null)) as { message?: string } | null;
        setError(body?.message ?? "No se pudo generar el archivo. Intenta de nuevo.");
        return;
      }

      save(await response.blob(), fileNameFrom(response.headers.get("content-disposition")) ?? job.fallbackName);
    } catch {
      setError("No se pudo conectar con el servidor. Intenta de nuevo.");
    } finally {
      setPending(null);
    }
  }

  return (
    <div className="flex flex-col items-end gap-1">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" disabled={!period || pending !== null} className="bg-surface-raised">
            {pending ? <LoaderCircle className="animate-spin" /> : <Download />}
            {pending ?? "Exportar"}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuLabel>PDF</DropdownMenuLabel>
          <DropdownMenuItem
            onSelect={() =>
              run({ href: href(slug, "pdf"), label: "Generando PDF…", fallbackName: `furnistore-${slug}.pdf` })
            }
          >
            <FileText />
            Descargar PDF
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuLabel>CSV</DropdownMenuLabel>
          <DropdownMenuItem
            disabled={!table}
            onSelect={() =>
              table &&
              run({
                href: href(slug, "csv", table.id),
                label: "Preparando CSV…",
                fallbackName: `furnistore-${slug}.csv`,
              })
            }
          >
            <FileSpreadsheet />
            Descargar tabla en CSV
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      {error && (
        <p role="alert" className="max-w-72 text-right text-xs text-brick">
          {error}
        </p>
      )}
    </div>
  );
}
