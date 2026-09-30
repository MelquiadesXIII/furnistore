import { toUserMessage, type AppError } from "@/lib/errors";

const ORDER_MESSAGES: Record<string, string> = {
  "checkout.client_not_found": "Esta cuenta no tiene datos de cliente asociados.",
  "checkout.empty_cart": "Tu carrito está vacío.",
  "checkout.profile_incomplete": "Completa tus datos de envío antes de confirmar el pedido.",
  "checkout.price_changed":
    "Los precios de tu carrito cambiaron. Revisa el nuevo total y confirma de nuevo.",
  "order.invalid_transition": "Este pedido ya no se puede cancelar.",
  "order.not_found": "No encontramos ese pedido.",
};

const SERVER_WORDED = new Set(["checkout.insufficient_stock", "checkout.product_unavailable"]);

export function orderErrorMessage(error: AppError): string {
  if (error.code && SERVER_WORDED.has(error.code) && error.messages[0]) {
    return error.messages[0];
  }

  return (error.code && ORDER_MESSAGES[error.code]) || toUserMessage(error);
}
