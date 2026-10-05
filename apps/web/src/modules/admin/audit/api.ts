import { authedApiFetch } from "@/lib/api/authed";
import { toQueryString } from "@/lib/api/query";
import type { ApiSchemas } from "@/lib/api/contract";
import type { Result } from "@/lib/result";

export type AuditPage = ApiSchemas["AuditEntryResponsePagedResult"];

export const AUDIT_PAGE_SIZE = 50;

export function getAuditEntries(query: {
  page: number;
  entityType?: string | null;
  entityId?: string | null;
  action?: string | null;
  pageSize?: number;
}): Promise<Result<AuditPage>> {
  return authedApiFetch<AuditPage>(
    `/api/admin/audit${toQueryString({
      page: query.page,
      pageSize: query.pageSize ?? AUDIT_PAGE_SIZE,
      entityType: query.entityType,
      entityId: query.entityId,
      action: query.action,
    })}`,
  );
}
