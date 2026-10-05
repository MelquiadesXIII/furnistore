import Link from "next/link";
import { notFound } from "next/navigation";
import { Panel } from "@/components/panel";
import { Card, CardContent } from "@/components/ui/card";
import { getAuditEntries } from "@/modules/admin/audit/api";
import { AuditEntryItem } from "@/modules/admin/audit/audit-entry-item";
import { getAdminCategories } from "@/modules/admin/categories/api";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { updateProduct } from "@/modules/admin/products/actions";
import { getAdminProduct } from "@/modules/admin/products/api";
import { DeleteProductButton } from "@/modules/admin/products/form/delete-product-button";
import { ProductEditor } from "@/modules/admin/products/form/product-editor";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { buildProductHref } from "@/modules/products/slug";

export async function EditProductContainer({ id }: { id: number }) {
  const [product, categories, history] = await Promise.all([
    getAdminProduct(id),
    getAdminCategories(),
    getAuditEntries({ page: 1, pageSize: 50, entityType: "Product", entityId: String(id) }),
  ]);

  if (!product.ok && product.error.kind === "notFound") notFound();

  if (!product.ok) return <Panel>{adminErrorMessage(product.error)}</Panel>;
  if (!categories.ok) return <Panel>{adminErrorMessage(categories.error)}</Panel>;

  const value = product.value;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/admin/products" className="text-sm text-ink-muted transition-colors hover:text-accent">
          ← Productos
        </Link>
        <AdminPageHeader
          title={value.name}
          description={value.isActive ? "Visible en la tienda." : "Archivado: no aparece en la tienda."}
          actions={
            value.isActive && (
              <Link
                href={buildProductHref(value)}
                className="text-sm text-accent hover:underline"
                target="_blank"
              >
                Ver en la tienda
              </Link>
            )
          }
        />
      </div>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <Card className="rounded-sm">
          <CardContent>
            <ProductEditor
              product={value}
              categories={categories.value.items}
              action={updateProduct.bind(null, value.id, value.version)}
            />
          </CardContent>
        </Card>

        <div className="flex flex-col gap-6">
          {!value.hasOrders && <DeleteProductButton productId={value.id} name={value.name} />}
          <section className="flex flex-col gap-3">
            <h2 className="font-display text-base font-semibold text-ink">Historial</h2>
            <Card className="rounded-sm">
              <CardContent>
                {!history.ok || history.value.items.length === 0 ? (
                  <p className="text-sm text-ink-muted">Sin cambios registrados.</p>
                ) : (
                  <ul className="divide-y divide-hairline">
                    {history.value.items.map((entry) => (
                      <AuditEntryItem key={entry.id} entry={entry} />
                    ))}
                  </ul>
                )}
              </CardContent>
            </Card>
          </section>
        </div>
      </div>
    </div>
  );
}
