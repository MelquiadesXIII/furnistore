import { seriesTone, type ReportBlock } from "@/modules/admin/reports/document";
import { formatBucket, formatValue } from "@/modules/admin/reports/format";
import type { ProductsReport } from "@/modules/admin/reports/types";
import { productsTables } from "@/modules/admin/reports/views/products/products-tables";

const TOP_PRODUCTS = 10;
const NO_SALES = "No hubo ventas en este período.";

export function productsDocument(report: ProductsReport): ReportBlock[] {
  const sold = report.categories.filter((category) => category.revenue > 0);
  const leader = report.products[0];

  return [
    {
      type: "cards",
      columns: 4,
      cards: [
        { type: "stat", label: "Ingresos por productos", value: formatValue("money", report.revenue), hint: "Sin envío" },
        { type: "stat", label: "Productos vendidos", value: formatValue("count", report.products.length) },
        {
          type: "stat",
          label: "Activos sin ventas",
          value: formatValue("count", report.unsold.length),
          hint: "Ver la tabla al final",
        },
        {
          type: "stat",
          label: "Más vendido",
          value: leader ? formatValue("percent", leader.share) : "—",
          hint: leader ? `${leader.name}, del total` : "Sin ventas",
        },
      ],
    },
    {
      type: "pair",
      blocks: [
        {
          type: "chart",
          title: `Top ${TOP_PRODUCTS} por ingresos`,
          empty: NO_SALES,
          chart:
            report.products.length === 0
              ? null
              : {
                  kind: "bars",
                  layout: "horizontal",
                  valueKind: "money",
                  valueLabel: "Ingresos",
                  items: report.products
                    .slice(0, TOP_PRODUCTS)
                    .map((row) => ({ label: row.name, value: row.revenue, tone: "chart-1" })),
                },
        },
        {
          type: "chart",
          title: "Participación por categoría",
          empty: NO_SALES,
          chart:
            sold.length === 0
              ? null
              : {
                  kind: "donut",
                  valueKind: "money",
                  items: sold.map((category, index) => ({
                    label: category.name,
                    value: category.revenue,
                    share: category.share,
                    tone: seriesTone(index),
                  })),
                },
        },
      ],
    },
    {
      type: "chart",
      title: "Ingresos por categoría en el tiempo",
      empty: NO_SALES,
      chart:
        sold.length === 0
          ? null
          : {
              kind: "stacked",
              valueKind: "money",
              series: sold.map((category, index) => ({ label: category.name, tone: seriesTone(index) })),
              points: report.categorySeries.map((point) => ({
                label: formatBucket(point.start, point.end, report.meta.groupBy),
                values: sold.map(
                  (category) =>
                    point.categories.find((entry) => entry.categoryId === category.categoryId)?.revenue ?? 0,
                ),
              })),
            },
    },
    ...productsTables(report).map((table) => ({ type: "table" as const, table })),
  ];
}
