import { createLoader, parseAsString, type SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { getAdminCategories } from "@/modules/admin/categories/api";
import { CategoriesTable } from "@/modules/admin/categories/categories-table";
import { NewCategoryForm } from "@/modules/admin/categories/new-category-form";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { SearchFilter } from "@/modules/admin/table/table-filters";
import { TableFrame } from "@/modules/admin/table/table-frame";

const loadParams = createLoader({
  q: parseAsString.withDefault(""),
  sort: parseAsString.withDefault("name"),
});

export async function CategoriesContainer({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const { q, sort } = await loadParams(searchParams);
  const result = await getAdminCategories(q, sort);

  return (
    <div>
      <AdminPageHeader
        title="Categorías"
        description="Una categoría solo se puede borrar cuando no tiene productos."
        actions={<NewCategoryForm />}
      />
      <TableFrame>
        <SearchFilter placeholder="Buscar categoría" />
        {result.ok ? (
          <CategoriesTable categories={result.value.items} />
        ) : (
          <Panel>{adminErrorMessage(result.error)}</Panel>
        )}
      </TableFrame>
    </div>
  );
}
