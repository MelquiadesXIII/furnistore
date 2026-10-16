export function formatPrice(value: number): string {
  const decimals = Number.isInteger(value) ? 0 : 2;
  return `${value.toLocaleString("es", { minimumFractionDigits: decimals, maximumFractionDigits: decimals })} CUP`;
}
