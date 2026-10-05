import { toUserMessage, type AppError } from "@/lib/errors";

export function adminErrorMessage(error: AppError): string {
  if (error.code && error.messages[0]) return error.messages[0];
  return toUserMessage(error);
}
