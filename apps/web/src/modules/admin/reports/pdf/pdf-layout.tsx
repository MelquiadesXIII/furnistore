import { Document, Font, Page, StyleSheet, Text, View } from "@react-pdf/renderer";
import type { ReactNode } from "react";
import { formatCalendarDate, formatMoment } from "@/lib/format-date";
import { formatRange, formatValue } from "@/modules/admin/reports/format";
import { CONTENT_WIDTH, PAGE_MARGIN, PDF_COLORS } from "@/modules/admin/reports/pdf/theme";
import { isNumericKind, type CellKind, type CellValue, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { ReportMeta } from "@/modules/admin/reports/types";

Font.registerHyphenationCallback((word) => [word]);

export const CHART_WIDTH = CONTENT_WIDTH - 24;

const styles = StyleSheet.create({
  page: { padding: PAGE_MARGIN, paddingBottom: PAGE_MARGIN + 14, fontFamily: "Helvetica", fontSize: 8, color: PDF_COLORS.ink },
  brand: { fontFamily: "Helvetica-Bold", fontSize: 11 },
  title: { fontFamily: "Helvetica-Bold", fontSize: 20, marginTop: 10 },
  subtitle: { fontSize: 9, color: PDF_COLORS.muted, marginTop: 3 },
  header: { borderBottomWidth: 1, borderBottomColor: PDF_COLORS.ink, paddingBottom: 10, marginBottom: 14 },
  stats: { flexDirection: "row", gap: 8, marginBottom: 14 },
  stat: { flex: 1, padding: 8, borderWidth: 0.75, borderColor: PDF_COLORS.hairline, backgroundColor: PDF_COLORS.raised },
  statLabel: { fontSize: 7, color: PDF_COLORS.muted, textTransform: "uppercase" },
  statValue: { fontFamily: "Helvetica-Bold", fontSize: 13, marginTop: 3 },
  box: { padding: 12, borderWidth: 0.75, borderColor: PDF_COLORS.hairline, backgroundColor: PDF_COLORS.raised, marginBottom: 14 },
  boxTitle: { fontFamily: "Helvetica-Bold", fontSize: 10, marginBottom: 8 },
  tableTitle: { fontFamily: "Helvetica-Bold", fontSize: 10, marginBottom: 4 },
  headerRow: { flexDirection: "row", borderBottomWidth: 0.75, borderBottomColor: PDF_COLORS.ink, paddingBottom: 3 },
  headerCell: { fontSize: 7, color: PDF_COLORS.muted, textTransform: "uppercase", paddingHorizontal: 3 },
  row: { flexDirection: "row", borderBottomWidth: 0.5, borderBottomColor: PDF_COLORS.hairline, paddingVertical: 3 },
  cell: { fontSize: 8, paddingHorizontal: 3 },
  empty: { fontSize: 8, color: PDF_COLORS.muted },
  footer: {
    position: "absolute",
    bottom: PAGE_MARGIN - 10,
    left: PAGE_MARGIN,
    right: PAGE_MARGIN,
    flexDirection: "row",
    justifyContent: "space-between",
    fontSize: 7,
    color: PDF_COLORS.muted,
  },
});

export function PdfReport({
  title,
  meta,
  generatedBy,
  usesPeriod,
  children,
}: {
  title: string;
  meta: ReportMeta;
  generatedBy: string;
  usesPeriod: boolean;
  children: ReactNode;
}) {
  const range = formatRange(meta.period.from, meta.period.to);

  return (
    <Document title={`Reporte de ${title}`} author="Furnistore" language="es">
      <Page size="A4" style={styles.page}>
        <View style={styles.header}>
          <Text style={styles.brand}>Furnistore</Text>
          <Text style={styles.title}>Reporte de {title.toLowerCase()}</Text>
          <Text style={styles.subtitle}>
            {usesPeriod ? `Período: ${range}` : "Stock al día de hoy"} · Generado el {formatMoment(meta.generatedAt)} por{" "}
            {generatedBy} · Importes en {meta.currency}
          </Text>
        </View>
        {children}
        <View style={styles.footer} fixed>
          <Text>Furnistore · Reporte de {title.toLowerCase()}</Text>
          <Text render={({ pageNumber, totalPages }) => `Página ${pageNumber} de ${totalPages}`} />
        </View>
      </Page>
    </Document>
  );
}

export function PdfStats({ stats }: { stats: { label: string; value: string }[] }) {
  return (
    <View style={styles.stats}>
      {stats.map((stat) => (
        <View key={stat.label} style={styles.stat}>
          <Text style={styles.statLabel}>{stat.label}</Text>
          <Text style={styles.statValue}>{stat.value}</Text>
        </View>
      ))}
    </View>
  );
}

export function PdfChartBox({ title, empty, children }: { title: string; empty?: string; children: ReactNode }) {
  return (
    <View style={styles.box} wrap={false}>
      <Text style={styles.boxTitle}>{title}</Text>
      {empty ? <Text style={styles.empty}>{empty}</Text> : children}
    </View>
  );
}

function cellText(kind: CellKind, value: CellValue): string {
  if (value === null || value === "") return "—";
  if (kind === "text") return String(value);
  if (kind === "date") return formatCalendarDate(String(value));
  if (kind === "moment") return formatMoment(String(value));
  return formatValue(kind, Number(value));
}

export function PdfTable({ table }: { table: ReportTableData }) {
  const flex = (index: number) => (index === 0 ? 2 : table.columns[index].kind === "text" ? 1.5 : 1);
  const align = (index: number) => (isNumericKind(table.columns[index].kind) ? ("right" as const) : ("left" as const));

  return (
    <View>
      <Text style={styles.tableTitle} minPresenceAhead={40}>
        {table.title}
      </Text>
      {table.rows.length === 0 ? (
        <Text style={styles.empty}>{table.empty}</Text>
      ) : (
        <View>
          <View style={styles.headerRow} fixed>
            {table.columns.map((column, index) => (
              <Text key={column.header} style={[styles.headerCell, { flex: flex(index), textAlign: align(index) }]}>
                {column.header}
              </Text>
            ))}
          </View>
          {table.rows.map((row, rowIndex) => (
            <View key={rowIndex} style={styles.row} wrap={false}>
              {row.cells.map((value, index) => (
                <Text key={index} style={[styles.cell, { flex: flex(index), textAlign: align(index) }]}>
                  {cellText(table.columns[index].kind, value)}
                </Text>
              ))}
            </View>
          ))}
        </View>
      )}
    </View>
  );
}
