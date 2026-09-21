import { toUserMessage, type AppError } from "@/lib/errors";

const CART_MESSAGES: Record<string, string> = {
  "cart.insufficient_stock": "No hay suficiente stock para esa cantidad.",
  "cart.item_not_found": "Ese producto ya no está en tu carrito.",
  "cart.product_not_found": "Ese producto ya no está disponible.",
  "cart.product_unavailable": "Ese producto ya no está disponible.",
};

export function cartErrorMessage(error: AppError): string {
  return (error.code && CART_MESSAGES[error.code]) || toUserMessage(error);
}
