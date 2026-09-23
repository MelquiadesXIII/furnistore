export type ConfirmationOutcome = {
  succeeded: boolean;
  title: string;
  message: string;
  action: { href: string; label: string };
};

const RETRY_FROM_LOGIN = "Inicia sesión con tu correo y contraseña para pedir un enlace nuevo.";

const OUTCOMES: Record<string, ConfirmationOutcome> = {
  success: {
    succeeded: true,
    title: "Correo confirmado",
    message: "Tu cuenta ya está activa. Inicia sesión para empezar a comprar.",
    action: { href: "/login", label: "Iniciar sesión ahora" },
  },
  invalid: {
    succeeded: false,
    title: "Enlace no válido",
    message: `El enlace de confirmación no es válido. ${RETRY_FROM_LOGIN}`,
    action: { href: "/login", label: "Ir a iniciar sesión" },
  },
  "not-found": {
    succeeded: false,
    title: "Cuenta no encontrada",
    message: "No encontramos la cuenta asociada a este enlace. Puedes crear una cuenta nueva.",
    action: { href: "/register", label: "Crear cuenta" },
  },
  failed: {
    succeeded: false,
    title: "No pudimos confirmar tu correo",
    message: `El enlace venció o ya no es válido. ${RETRY_FROM_LOGIN}`,
    action: { href: "/login", label: "Ir a iniciar sesión" },
  },
};

const UNKNOWN: ConfirmationOutcome = {
  succeeded: false,
  title: "No pudimos verificar el resultado",
  message: `Abre de nuevo el enlace de tu correo. ${RETRY_FROM_LOGIN}`,
  action: { href: "/login", label: "Ir a iniciar sesión" },
};

export function confirmationOutcome(status: unknown): ConfirmationOutcome {
  return typeof status === "string" && Object.hasOwn(OUTCOMES, status) ? OUTCOMES[status] : UNKNOWN;
}
