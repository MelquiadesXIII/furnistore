import { toUserMessage, type AppError } from "@/lib/errors";

const ACCOUNT_MESSAGES: Record<string, string> = {
  "client.self_not_found": "Esta cuenta no tiene datos de cliente asociados.",
};

export function accountErrorMessage(error: AppError): string {
  return (error.code && ACCOUNT_MESSAGES[error.code]) || toUserMessage(error);
}
