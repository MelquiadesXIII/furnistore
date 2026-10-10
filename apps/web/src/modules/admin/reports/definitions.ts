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

export type ReportDefinition = {
  kind: ReportKind;
  title: string;
  fileName: string;
  summary: string;
  usesPeriod: boolean;
  hasGrouping: boolean;
};

export const REPORTS: Record<ReportSlug, ReportDefinition> = {
  sales: {
    kind: "Sales",
    title: "Ventas",
    fileName: "ventas",
    summary: "Cuánto se vendió en el período.",
    usesPeriod: true,
    hasGrouping: true,
  },
  products: {
    kind: "Products",
    title: "Productos",
    fileName: "productos",
    summary: "Qué productos se vendieron más.",
    usesPeriod: true,
    hasGrouping: false,
  },
  operations: {
    kind: "Operations",
    title: "Operación",
    fileName: "operacion",
    summary: "Estado de los pedidos y entregas.",
    usesPeriod: true,
    hasGrouping: false,
  },
  cancellations: {
    kind: "Cancellations",
    title: "Cancelaciones",
    fileName: "cancelaciones",
    summary: "Pedidos cancelados y quién los canceló.",
    usesPeriod: true,
    hasGrouping: false,
  },
  inventory: {
    kind: "Inventory",
    title: "Inventario",
    fileName: "inventario",
    summary: "Stock actual de los productos activos.",
    usesPeriod: false,
    hasGrouping: false,
  },
  customers: {
    kind: "Customers",
    title: "Clientes",
    fileName: "clientes",
    summary: "Quiénes compraron y desde dónde.",
    usesPeriod: true,
    hasGrouping: false,
  },
};

export function isReportSlug(value: string): value is ReportSlug {
  return (REPORT_SLUGS as readonly string[]).includes(value);
}
