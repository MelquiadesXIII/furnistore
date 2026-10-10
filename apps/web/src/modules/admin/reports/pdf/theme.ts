import type { Tone } from "@/modules/admin/reports/document";

export const PDF_COLORS = {
  ink: "#211c16",
  muted: "#6b6255",
  hairline: "#ddd5c6",
  raised: "#faf7f2",
  accent: "#8f6526",
  moss: "#4f6b3a",
  brick: "#a83a26",
  white: "#ffffff",
};

const TONES: Record<Tone, string> = {
  "chart-1": "#8f6526",
  "chart-2": "#4f6b3a",
  "chart-3": "#3f6475",
  "chart-4": "#b9814a",
  "chart-5": "#7a4b6b",
  "chart-6": "#a83a26",
  "chart-7": "#8a8a4a",
  "chart-8": "#5b5049",
  muted: "#6b6255",
};

export function pdfTone(tone: Tone): string {
  return TONES[tone];
}

export const PAGE_MARGIN = 36;
export const CONTENT_WIDTH = 595.28 - PAGE_MARGIN * 2;
export const SECTION_PADDING = 12;
export const GAP = 10;
