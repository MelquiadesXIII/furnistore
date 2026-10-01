import { Pagination } from "@/components/pagination";
import { Panel } from "@/components/panel";
import { toUserMessage } from "@/lib/errors";
import { getSession } from "@/lib/session";
import { getProducts } from "@/modules/products/api";
import { ProductGrill } from "@/modules/products/list/product-grill";

const PAGE_SIZE = 12;

function buildHref(page: number, query: string) {
  const params = new URLSearchParams();
  if (query) params.set("q", query);
  if (page > 1) params.set("page", String(page));
  const qs = params.toString();
  return qs ? `/?${qs}` : "/";
}

export async function ProductContainer({
  query,
  page,
}: { query?: string; page?: string } = {}) {
  const isAuthenticated = Boolean(await getSession());

  const trimmedQuery = query?.trim() ?? "";
  const requestedPage = Number.parseInt(page ?? "1", 10);
  const currentPage = Number.isFinite(requestedPage) ? Math.max(1, requestedPage) : 1;

  const result = await getProducts({
    page: currentPage,
    pageSize: PAGE_SIZE,
    search: trimmedQuery || undefined,
  });

  const products = result.ok ? result.value.items : [];
  const total = result.ok ? result.value.total : 0;
  const totalPages = result.ok ? Math.max(1, result.value.totalPages) : 1;

  return (
    <div className="mx-auto w-full max-w-6xl flex-1 px-6 py-10">
      <div className="mb-8 border-b border-hairline pb-6">
        <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">Catálogo</h1>
        <p className="mt-1 text-sm text-ink-muted">
          {result.ok
            ? `${total} ${total === 1 ? "pieza disponible" : "piezas disponibles"}`
            : "No se pudo cargar el catálogo."}
        </p>
      </div>

      {!result.ok ? (
        <Panel>{toUserMessage(result.error)}</Panel>
      ) : products.length === 0 ? (
        <Panel>
          {trimmedQuery
            ? `Sin resultados para "${trimmedQuery}".`
            : "Todavía no hay piezas en el catálogo."}
        </Panel>
      ) : (
        <>
          <ProductGrill products={products} isAuthenticated={isAuthenticated} />
          {totalPages > 1 && (
            <Pagination
              currentPage={Math.min(currentPage, totalPages)}
              totalPages={totalPages}
              hrefFor={(target) => buildHref(target, trimmedQuery)}
            />
          )}
        </>
      )}
    </div>
  );
}
