"use client";

import { Plus } from "lucide-react";
import { useRef, useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { createCategory } from "@/modules/admin/categories/actions";

export function NewCategoryForm() {
  const formRef = useRef<HTMLFormElement>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  return (
    <form
      ref={formRef}
      className="flex flex-col gap-1"
      onSubmit={(event) => {
        event.preventDefault();
        const name = new FormData(event.currentTarget).get("name");
        setError(null);
        startTransition(async () => {
          const result = await createCategory(name);
          if (result.ok) formRef.current?.reset();
          else setError(result.message);
        });
      }}
    >
      <div className="flex items-center gap-2">
        <Input
          name="name"
          aria-label="Nombre de la nueva categoría"
          placeholder="Nueva categoría"
          required
          minLength={2}
          maxLength={60}
          className="bg-surface-raised sm:w-56"
        />
        <Button type="submit" disabled={pending}>
          <Plus />
          {pending ? "Creando…" : "Crear"}
        </Button>
      </div>
      {error && (
        <span role="alert" className="text-xs text-brick">
          {error}
        </span>
      )}
    </form>
  );
}
