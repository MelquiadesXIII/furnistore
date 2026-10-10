import type { ReportKind } from "@/modules/admin/reports/types";

export const REPORT_SLUGS = [
  "sales",
  "products",
  "operations",
  "cancellations",
  "inventory",
  "customers",
] as const;

export const REPORT_GROUPINGS = ["Day", "Week", "Month"] as const;

export type ReportSlug = (typeof REPORT_SLUGS)[number];

export type ExportTarget = ReportSlug | "all";

export type ReportDefinition = {
  kind: ReportKind;
  title: string;
  fileName: string;
  summary: string;
  hasSeries: boolean;
};

export const REPORTS: Record<ReportSlug, ReportDefinition> = {
  sales: {
    kind: "Sales",
    title: "Ventas",
    fileName: "ventas",
    summary: "Ingresos, pedidos, ticket promedio y unidades, comparados con el período anterior.",
    hasSeries: true,
  },
  products: {
    kind: "Products",
    title: "Productos y categorías",
    fileName: "productos",
    summary: "Qué se vende, cuánto pesa cada categoría y qué muebles no se mueven.",
    hasSeries: true,
  },
  operations: {
    kind: "Operations",
    title: "Operación y entregas",
    fileName: "operacion",
    summary: "Estados de los pedidos, tiempos de cada etapa y entregas a tiempo.",
    hasSeries: false,
  },
  cancellations: {
    kind: "Cancellations",
    title: "Cancelaciones",
    fileName: "cancelaciones",
    summary: "Cuántos pedidos se cancelan, quién los cancela, en qué etapa y por qué.",
    hasSeries: false,
  },
  inventory: {
    kind: "Inventory",
    title: "Inventario",
    fileName: "inventario",
    summary: "Stock actual, valor del inventario y qué se va a agotar al ritmo de venta del período.",
    hasSeries: false,
  },
  customers: {
    kind: "Customers",
    title: "Clientes y zonas",
    fileName: "clientes",
    summary: "Compradores nuevos y recurrentes, mejores clientes y ventas por provincia y ciudad.",
    hasSeries: true,
  },
};

export const FULL_REPORT_TITLE = "Informe completo";

export function isReportSlug(value: string): value is ReportSlug {
  return (REPORT_SLUGS as readonly string[]).includes(value);
}

export function isExportTarget(value: string): value is ExportTarget {
  return value === "all" || isReportSlug(value);
}

export function exportTitle(target: ExportTarget): string {
  return target === "all" ? FULL_REPORT_TITLE : REPORTS[target].title;
}

export function exportFileName(target: ExportTarget): string {
  return target === "all" ? "informe-completo" : REPORTS[target].fileName;
}

export function exportKind(target: ExportTarget): ReportKind {
  return target === "all" ? "All" : REPORTS[target].kind;
}
