"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { deleteCategory, renameCategory } from "@/modules/admin/categories/actions";
import type { AdminCategory } from "@/modules/admin/categories/types";

export function CategoryRowActions({ category }: { category: AdminCategory }) {
  const [mode, setMode] = useState<"idle" | "rename" | "delete">("idle");
  const [name, setName] = useState(category.name);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function run(action: () => ReturnType<typeof deleteCategory>) {
    setError(null);
    startTransition(async () => {
      const result = await action();
      if (result.ok) setMode("idle");
      else setError(result.message);
    });
  }

  return (
    <div className="flex flex-col items-end gap-1">
      {mode === "rename" ? (
        <form
          className="flex items-center gap-2"
          onSubmit={(event) => {
            event.preventDefault();
            run(() => renameCategory(category.id, name));
          }}
        >
          <Input
            aria-label={`Nuevo nombre para ${category.name}`}
            value={name}
            maxLength={60}
            disabled={pending}
            onChange={(event) => setName(event.target.value)}
            className="h-7 w-44 bg-surface-raised"
          />
          <Button type="submit" size="sm" disabled={pending}>
            Guardar
          </Button>
          <Button type="button" size="sm" variant="ghost" disabled={pending} onClick={() => setMode("idle")}>
            Cancelar
          </Button>
        </form>
      ) : mode === "delete" ? (
        <div className="flex items-center gap-2">
          <span className="text-xs text-ink">¿Borrar «{category.name}»?</span>
          <Button
            type="button"
            size="sm"
            variant="destructive"
            disabled={pending}
            onClick={() => run(() => deleteCategory(category.id))}
          >
            Borrar
          </Button>
          <Button type="button" size="sm" variant="ghost" disabled={pending} onClick={() => setMode("idle")}>
            No
          </Button>
        </div>
      ) : (
        <div className="flex items-center gap-1">
          <Button type="button" size="sm" variant="ghost" onClick={() => setMode("rename")}>
            Renombrar
          </Button>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={category.productCount > 0}
            title={category.productCount > 0 ? "Solo se pueden borrar categorías sin productos" : undefined}
            onClick={() => setMode("delete")}
          >
            Borrar
          </Button>
        </div>
      )}
      {error && (
        <span role="alert" className="text-xs text-brick">
          {error}
        </span>
      )}
    </div>
  );
}
