import { createLoader, createParser, createSerializer, parseAsStringLiteral } from "nuqs/server";
import { REPORT_GROUPINGS } from "@/modules/admin/reports/definitions";

const DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export const parseAsDay = createParser({
  parse: (value: string) => {
    if (!DAY_PATTERN.test(value)) return null;
    const date = new Date(`${value}T00:00:00Z`);
    return Number.isNaN(date.getTime()) || date.toISOString().slice(0, 10) !== value ? null : value;
  },
  serialize: (value: string) => value,
});

export const reportSearchParams = {
  from: parseAsDay,
  to: parseAsDay,
  groupBy: parseAsStringLiteral(REPORT_GROUPINGS),
};

export const loadReportSearchParams = createLoader(reportSearchParams);

export const serializeReportQuery = createSerializer(reportSearchParams);
