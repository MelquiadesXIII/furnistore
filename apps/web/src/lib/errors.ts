export type AppErrorKind =
  | "network"
  | "timeout"
  | "unauthorized"
  | "notFound"
  | "conflict"
  | "validation"
  | "server"
  | "unexpected";

export type AppError = {
  kind: AppErrorKind;
  status?: number;
  code?: string;
  messages: string[];
};

const USER_MESSAGES: Record<AppErrorKind, string> = {
  network: "No se pudo conectar con el servidor. Intenta de nuevo en un momento.",
  timeout: "El servidor tardó demasiado en responder. Intenta de nuevo.",
  unauthorized: "Tu sesión expiró. Vuelve a iniciar sesión.",
  notFound: "No encontramos lo que buscabas.",
  conflict: "Los datos cambiaron mientras tanto. Revisa e intenta de nuevo.",
  validation: "Revisa los datos ingresados.",
  server: "El servidor tuvo un problema. Intenta de nuevo en un momento.",
  unexpected: "Ocurrió un error inesperado.",
};

export function toUserMessage(error: AppError): string {
  return USER_MESSAGES[error.kind];
}
