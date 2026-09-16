"use client";

import { Search } from "lucide-react";
import { debounce, parseAsInteger, parseAsString, useQueryStates } from "nuqs";
import { Input } from "@/components/ui/input";

const SEARCH_DEBOUNCE_MS = 300;

export function ProductSearchBar() {
  const [{ q }, setSearch] = useQueryStates(
    {
      q: parseAsString.withDefault(""),
      page: parseAsInteger.withDefault(1),
    },
    {
      shallow: false,
      clearOnDefault: true,
      limitUrlUpdates: debounce(SEARCH_DEBOUNCE_MS),
    },
  );

  return (
    <div className="relative w-full max-w-md">
      <Search className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-ink-muted" />
      <Input
        type="search"
        value={q}
        onChange={(event) => setSearch({ q: event.target.value, page: null })}
        aria-label="Buscar piezas"
        placeholder="Buscar piezas..."
        className="bg-surface pl-9"
      />
    </div>
  );
}
