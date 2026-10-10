import "server-only";

import { Document, Font, Page, renderToBuffer, StyleSheet, Text, View } from "@react-pdf/renderer";
import { formatCalendarDate, formatMoment } from "@/lib/format-date";
import { changeTone, type Card, type ChartBlock, type ReportBlock } from "@/modules/admin/reports/document";
import { formatChange, formatRange, formatValue, GROUPING_LABELS } from "@/modules/admin/reports/format";
import { PdfChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { CONTENT_WIDTH, GAP, PAGE_MARGIN, PDF_COLORS, SECTION_PADDING } from "@/modules/admin/reports/pdf/theme";
import { isNumericKind, type CellKind, type CellValue, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { ReportMeta } from "@/modules/admin/reports/types";

Font.registerHyphenationCallback((word) => [word]);

export type PdfSection = { title: string; summary: string; blocks: ReportBlock[] };

const styles = StyleSheet.create({
  page: {
    paddingTop: PAGE_MARGIN,
    paddingBottom: PAGE_MARGIN + 14,
    paddingHorizontal: PAGE_MARGIN,
    fontFamily: "Helvetica",
    fontSize: 8,
    color: PDF_COLORS.ink,
  },
  brandRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  brand: { fontFamily: "Helvetica-Bold", fontSize: 12, color: PDF_COLORS.ink },
  eyebrow: { fontSize: 7, color: PDF_COLORS.muted, textTransform: "uppercase", letterSpacing: 0.8 },
  title: { fontFamily: "Helvetica-Bold", fontSize: 22, marginTop: 14 },
  period: { fontSize: 9, marginTop: 4, color: PDF_COLORS.ink },
  periodMuted: { color: PDF_COLORS.muted },
  metaRow: {
    flexDirection: "row",
    marginTop: 12,
    paddingBottom: 12,
    borderBottomWidth: 1.5,
    borderBottomColor: PDF_COLORS.ink,
  },
  metaCell: { flex: 1, gap: 2 },
  metaLabel: { fontSize: 7, color: PDF_COLORS.muted },
  sectionTitle: { fontFamily: "Helvetica-Bold", fontSize: 16 },
  sectionSummary: { fontSize: 8, color: PDF_COLORS.muted, marginTop: 2 },
  cardsRow: { flexDirection: "row", gap: GAP },
  card: {
    flex: 1,
    padding: 9,
    gap: 3,
    borderWidth: 0.75,
    borderColor: PDF_COLORS.hairline,
    backgroundColor: PDF_COLORS.raised,
  },
  cardLabel: { fontSize: 6.5, color: PDF_COLORS.muted, textTransform: "uppercase", letterSpacing: 0.6 },
  cardValue: { fontSize: 14, fontFamily: "Helvetica-Bold" },
  cardHint: { fontSize: 6.5, color: PDF_COLORS.muted },
  box: {
    padding: SECTION_PADDING,
    gap: 8,
    borderWidth: 0.75,
    borderColor: PDF_COLORS.hairline,
    backgroundColor: PDF_COLORS.raised,
  },
  boxTitle: { fontFamily: "Helvetica-Bold", fontSize: 10 },
  boxDescription: { fontSize: 7, color: PDF_COLORS.muted, marginTop: 2 },
  empty: { fontSize: 8, color: PDF_COLORS.muted },
  tableTitle: { fontFamily: "Helvetica-Bold", fontSize: 10 },
  tableHeader: {
    flexDirection: "row",
    borderBottomWidth: 0.75,
    borderBottomColor: PDF_COLORS.ink,
    paddingBottom: 3,
    marginTop: 6,
  },
  tableHeaderCell: { fontSize: 6.5, color: PDF_COLORS.muted, textTransform: "uppercase", paddingHorizontal: 3 },
  tableRow: {
    flexDirection: "row",
    borderBottomWidth: 0.5,
    borderBottomColor: PDF_COLORS.hairline,
    paddingVertical: 3.5,
  },
  tableCell: { fontSize: 7.5, paddingHorizontal: 3 },
  footer: {
    position: "absolute",
    bottom: PAGE_MARGIN - 8,
    left: PAGE_MARGIN,
    right: PAGE_MARGIN,
    flexDirection: "row",
    justifyContent: "space-between",
    fontSize: 7,
    color: PDF_COLORS.muted,
  },
});

function cellText(kind: CellKind, value: CellValue): string {
  if (value === null || value === "") return "—";
  if (kind === "text") return String(value);
  if (kind === "date") return formatCalendarDate(String(value));
  if (kind === "moment") return formatMoment(String(value));
  return formatValue(kind, Number(value));
}

function PdfCard({ card }: { card: Card }) {
  if (card.type === "stat") {
    return (
      <View style={styles.card}>
        <Text style={styles.cardLabel}>{card.label}</Text>
        <Text style={styles.cardValue}>{card.value}</Text>
        {card.hint && <Text style={styles.cardHint}>{card.hint}</Text>}
      </View>
    );
  }

  const tone = changeTone(card);
  const color = tone === "good" ? PDF_COLORS.moss : tone === "bad" ? PDF_COLORS.brick : PDF_COLORS.muted;

  return (
    <View style={styles.card}>
      <Text style={styles.cardLabel}>{card.label}</Text>
      <Text style={styles.cardValue}>{formatValue(card.kind, card.metric.value)}</Text>
      <Text style={styles.cardHint}>
        <Text style={{ color, fontFamily: "Helvetica-Bold" }}>{formatChange(card.metric.change)}</Text>
        {`  antes ${formatValue(card.kind, card.metric.previous)}`}
      </Text>
    </View>
  );
}

function PdfChartBox({ block, width }: { block: ChartBlock; width: number }) {
  return (
    <View style={[styles.box, { width }]} wrap={false}>
      <View>
        <Text style={styles.boxTitle}>{block.title}</Text>
        {block.description && <Text style={styles.boxDescription}>{block.description}</Text>}
      </View>
      {block.chart ? (
        <PdfChart spec={block.chart} width={width - SECTION_PADDING * 2} />
      ) : (
        <Text style={styles.empty}>{block.empty}</Text>
      )}
    </View>
  );
}

function columnWeight(kind: CellKind, index: number): number {
  if (index === 0) return 2.4;
  if (kind === "text") return 1.8;
  if (kind === "moment" || kind === "date") return 1.5;
  return 1.1;
}

function PdfTable({ table }: { table: ReportTableData }) {
  const weights = table.columns.map((column, index) => columnWeight(column.kind, index));
  const cell = (index: number) => ({
    flex: weights[index],
    textAlign: isNumericKind(table.columns[index].kind) ? ("right" as const) : ("left" as const),
  });

  return (
    <View style={{ marginTop: 4 }}>
      <View minPresenceAhead={60}>
        <Text style={styles.tableTitle}>{table.title}</Text>
        {table.description && <Text style={styles.boxDescription}>{table.description}</Text>}
      </View>
      {table.rows.length === 0 ? (
        <Text style={[styles.empty, { marginTop: 6 }]}>{table.empty}</Text>
      ) : (
        <View>
          <View style={styles.tableHeader} fixed>
            {table.columns.map((column, index) => (
              <Text key={column.header} style={[styles.tableHeaderCell, cell(index)]}>
                {column.header}
              </Text>
            ))}
          </View>
          {table.rows.map((row, rowIndex) => (
            <View key={rowIndex} style={styles.tableRow} wrap={false}>
              {row.cells.map((value, index) => (
                <Text key={index} style={[styles.tableCell, cell(index)]}>
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

function PdfBlocks({ blocks }: { blocks: ReportBlock[] }) {
  const half = (CONTENT_WIDTH - GAP) / 2;

  return (
    <View style={{ gap: 12 }}>
      {blocks.map((block, index) => {
        switch (block.type) {
          case "cards":
            return (
              <View key={index} style={styles.cardsRow} wrap={false}>
                {block.cards.map((card) => (
                  <PdfCard key={card.label} card={card} />
                ))}
              </View>
            );
          case "chart":
            return <PdfChartBox key={index} block={block} width={CONTENT_WIDTH} />;
          case "pair":
            return (
              <View key={index} style={{ flexDirection: "row", gap: GAP }} wrap={false}>
                {block.blocks.map((child) => (
                  <PdfChartBox key={child.title} block={child} width={half} />
                ))}
              </View>
            );
          case "table":
            return <PdfTable key={block.table.id} table={block.table} />;
        }
      })}
    </View>
  );
}

function ReportPdf({
  title,
  meta,
  generatedBy,
  showGrouping,
  sections,
}: {
  title: string;
  meta: ReportMeta;
  generatedBy: string;
  showGrouping: boolean;
  sections: PdfSection[];
}) {
  const range = formatRange(meta.period.from, meta.period.to);
  const single = sections.length === 1;

  return (
    <Document title={`${title} · ${range}`} author="Furnistore" creator="Furnistore" language="es">
      <Page size="A4" style={styles.page}>
        <View style={styles.brandRow}>
          <Text style={styles.brand}>Furnistore</Text>
          <Text style={styles.eyebrow}>Reporte de gestión</Text>
        </View>
        <Text style={styles.title}>{title}</Text>
        <Text style={styles.period}>
          Del {range}
          <Text style={styles.periodMuted}>
            {` · comparado con el ${formatRange(meta.previousPeriod.from, meta.previousPeriod.to)}`}
            {showGrouping ? ` · ${GROUPING_LABELS[meta.groupBy].toLowerCase()}` : ""}
          </Text>
        </Text>
        <View style={styles.metaRow}>
          <View style={styles.metaCell}>
            <Text style={styles.metaLabel}>Generado</Text>
            <Text>{formatMoment(meta.generatedAt)}</Text>
          </View>
          <View style={styles.metaCell}>
            <Text style={styles.metaLabel}>Por</Text>
            <Text>{generatedBy}</Text>
          </View>
          <View style={styles.metaCell}>
            <Text style={styles.metaLabel}>Moneda y hora</Text>
            <Text>{meta.currency} · La Habana</Text>
          </View>
        </View>

        {sections.map((section, index) => (
          <View key={section.title} break={index > 0} style={{ marginTop: 16, gap: 12 }}>
            {single ? (
              <Text style={styles.sectionSummary}>{section.summary}</Text>
            ) : (
              <View>
                <Text style={styles.sectionTitle}>{section.title}</Text>
                <Text style={styles.sectionSummary}>{section.summary}</Text>
              </View>
            )}
            <PdfBlocks blocks={section.blocks} />
          </View>
        ))}

        <View style={styles.footer} fixed>
          <Text>{`Furnistore · ${title} · ${range}`}</Text>
          <Text render={({ pageNumber, totalPages }) => `Página ${pageNumber} de ${totalPages}`} />
        </View>
      </Page>
    </Document>
  );
}

export function renderReportPdf(input: {
  title: string;
  meta: ReportMeta;
  generatedBy: string;
  showGrouping: boolean;
  sections: PdfSection[];
}): Promise<Buffer> {
  return renderToBuffer(<ReportPdf {...input} />);
}
