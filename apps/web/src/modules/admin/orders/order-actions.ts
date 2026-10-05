import type { AdminOrderAction } from "@/modules/admin/orders/types";

export const ACTION_LABELS: Record<Exclude<AdminOrderAction, "Cancel">, { label: string; path: "prepare" | "ship" | "deliver" }> = {
  Prepare: { label: "Pasar a preparación", path: "prepare" },
  Ship: { label: "Marcar enviado", path: "ship" },
  Deliver: { label: "Confirmar entrega", path: "deliver" },
};

export function forwardAction(actions: AdminOrderAction[]) {
  const next = actions.find((action): action is Exclude<AdminOrderAction, "Cancel"> => action !== "Cancel");
  return next ? ACTION_LABELS[next] : null;
}
