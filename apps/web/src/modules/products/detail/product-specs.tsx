import type { Product } from "@/modules/products/types";

const DIMENSIONS = [
  ["Ancho", "widthCm"],
  ["Fondo", "depthCm"],
  ["Alto", "heightCm"],
] as const;

export function ProductSpecs({ product }: { product: Product }) {
  const specs = [
    ...DIMENSIONS.flatMap(([label, key]) => {
      const value = product[key];
      return value === null ? [] : [{ label, value: `${value} cm` }];
    }),
    ...(product.material ? [{ label: "Material", value: product.material }] : []),
  ];

  if (specs.length === 0) return null;

  return (
    <dl className="grid grid-cols-2 gap-x-6 gap-y-3 border-t border-hairline pt-4 sm:grid-cols-4">
      {specs.map((spec) => (
        <div key={spec.label} className="flex flex-col gap-0.5">
          <dt className="text-xs font-medium tracking-wide text-ink-muted uppercase">
            {spec.label}
          </dt>
          <dd className="text-sm text-ink">{spec.value}</dd>
        </div>
      ))}
    </dl>
  );
}
