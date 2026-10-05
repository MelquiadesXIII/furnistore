import Link from "next/link";
import { Panel } from "@/components/panel";
import { Card, CardContent } from "@/components/ui/card";
import { getAdminCategories } from "@/modules/admin/categories/api";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { createProduct } from "@/modules/admin/products/actions";
import { ProductForm } from "@/modules/admin/products/form/product-form";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";

export async function NewProductContainer() {
  const categories = await getAdminCategories();

  return (
    <div className="max-w-3xl">
      <Link href="/admin/products" className="text-sm text-ink-muted transition-colors hover:text-accent">
        ← Productos
      </Link>
      <AdminPageHeader title="Nuevo producto" />
      {!categories.ok ? (
        <Panel>{adminErrorMessage(categories.error)}</Panel>
      ) : categories.value.items.length === 0 ? (
        <Panel>
          Primero crea una categoría.{" "}
          <Link href="/admin/categories" className="font-medium text-accent hover:underline">
            Ir a categorías
          </Link>
        </Panel>
      ) : (
        <Card className="rounded-sm">
          <CardContent>
            <ProductForm categories={categories.value.items} action={createProduct} submitLabel="Crear producto" />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
