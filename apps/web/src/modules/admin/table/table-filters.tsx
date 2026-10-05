"use client";

import { Search } from "lucide-react";
import { debounce, parseAsInteger, parseAsString, useQueryStates } from "nuqs";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTableTransition } from "@/modules/admin/table/table-frame";

const SEARCH_DEBOUNCE_MS = 300;
const ALL = "__all__";

export function SearchFilter({ placeholder }: { placeholder: string }) {
  const startTransition = useTableTransition();
  const [{ q }, setParams] = useQueryStates(
    { q: parseAsString.withDefault(""), page: parseAsInteger },
    {
      shallow: false,
      clearOnDefault: true,
      startTransition,
      limitUrlUpdates: debounce(SEARCH_DEBOUNCE_MS),
    },
  );

  return (
    <div className="relative w-full sm:max-w-xs">
      <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-muted" />
      <Input
        type="search"
        value={q}
        aria-label={placeholder}
        placeholder={placeholder}
        onChange={(event) => setParams({ q: event.target.value, page: null })}
        className="bg-surface-raised pl-9"
      />
    </div>
  );
}

export function SelectFilter({
  param,
  label,
  allLabel,
  options,
}: {
  param: string;
  label: string;
  allLabel: string;
  options: { value: string; label: string }[];
}) {
  const startTransition = useTableTransition();
  const [params, setParams] = useQueryStates(
    { [param]: parseAsString, page: parseAsInteger },
    { shallow: false, clearOnDefault: true, startTransition },
  );
  const value = (params[param] as string | null) ?? ALL;
  const selected = options.find((option) => option.value === value)?.label ?? allLabel;

  return (
    <Select
      value={value}
      onValueChange={(next) => setParams({ [param]: next === ALL ? null : next, page: null })}
    >
      <SelectTrigger aria-label={label} className="w-full bg-surface-raised sm:w-48">
        <SelectValue>{selected}</SelectValue>
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={ALL}>{allLabel}</SelectItem>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
