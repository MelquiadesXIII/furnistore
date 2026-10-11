import type { ShippingAddress } from "@/modules/account/types";

export function ShippingDetails({
  name,
  phone,
  identityCard,
  address,
}: {
  name: string;
  phone: string;
  identityCard?: string | null;
  address: ShippingAddress;
}) {
  return (
    <address className="flex flex-col gap-0.5 text-sm text-ink not-italic">
      <span className="font-medium">{name}</span>
      <span>{address.street}</span>
      <span>
        {address.city}, {address.province}
      </span>
      <span className="text-ink-muted">{phone}</span>
      {identityCard && <span className="text-ink-muted">CI {identityCard}</span>}
      {address.deliveryNotes && (
        <span className="mt-2 text-ink-muted">{address.deliveryNotes}</span>
      )}
    </address>
  );
}
