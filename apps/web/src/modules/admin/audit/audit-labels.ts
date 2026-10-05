import type { AuditEntry } from "@/modules/admin/types";

const FIELD_LABELS: Record<string, string> = {
  status: "Estado",
  cancelReason: "Motivo",
  name: "Nombre",
  description: "Descripción",
  price: "Precio",
  stock: "Stock",
  category: "Categoría",
  imageUrl: "Imagen",
  widthCm: "Ancho (cm)",
  depthCm: "Fondo (cm)",
  heightCm: "Alto (cm)",
  material: "Material",
  isActive: "Activo",
  firstName: "Nombre",
  lastName: "Apellidos",
  phone: "Teléfono",
  street: "Dirección",
  city: "Ciudad",
  province: "Provincia",
  deliveryNotes: "Indicaciones",
};

const VALUE_LABELS: Record<string, string> = {
  true: "Sí",
  false: "No",
  Paid: "Pagado",
  Processing: "En preparación",
  Shipped: "Enviado",
  Delivered: "Entregado",
  Cancelled: "Cancelado",
};

export function fieldLabel(field: string): string {
  return FIELD_LABELS[field] ?? field;
}

export function valueLabel(value: string | null): string {
  if (value === null || value === "") return "—";
  return VALUE_LABELS[value] ?? value;
}

export function actorLabel(actor: AuditEntry["actor"]): string {
  if (actor.email) return actor.email;
  return actor.userId === "console" ? "Consola" : actor.userId;
}

export function describeChange(field: string, change: { from: string | null; to: string | null }): string {
  return change.from === null
    ? `${fieldLabel(field)}: ${valueLabel(change.to)}`
    : `${fieldLabel(field)}: ${valueLabel(change.from)} → ${valueLabel(change.to)}`;
}
