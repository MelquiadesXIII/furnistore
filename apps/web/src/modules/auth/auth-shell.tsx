import type { ReactNode } from "react";
import Image, { type StaticImageData } from "next/image";
import Link from "next/link";
import { ChairMark } from "@/components/furniture-marks";

export function AuthShell({
  title,
  children,
  footer,
  photo,
}: {
  title: string;
  children: ReactNode;
  footer: ReactNode;
  photo?: StaticImageData;
}) {
  return (
    <div className="flex flex-1 flex-col md:flex-row">
      <div
        className={`relative flex flex-col gap-6 overflow-hidden bg-surface-raised px-8 py-10 md:w-1/2 md:px-16 ${
          photo
            ? "min-h-[38vh] justify-start md:min-h-0 md:py-16"
            : "justify-center border-b border-hairline md:border-b-0 md:border-r"
        }`}
      >
        {photo && (
          <>
            <Image
              src={photo}
              alt=""
              fill
              priority
              sizes="(min-width: 768px) 50vw, 100vw"
              className="auth-photo object-cover"
            />
            <div className="auth-photo-veil pointer-events-none absolute inset-0" />
            <div className="auth-photo-depth pointer-events-none absolute inset-0" />
          </>
        )}
        {!photo && <ChairMark className="relative z-10 h-16 w-16 text-accent md:h-24 md:w-24" />}
        <div className="relative z-10">
          {photo && <ChairMark className="mb-3 h-10 w-10 text-accent md:h-12 md:w-12" />}
          <Link
            href="/"
            className="font-display text-3xl font-semibold tracking-tight text-ink transition-colors hover:text-accent md:text-4xl"
          >
            Furnistore
          </Link>
          <p className="mt-2 max-w-xs font-mono text-xs uppercase tracking-widest text-ink-muted">
            Piezas hechas para durar
          </p>
        </div>
      </div>

      <div
        className={`flex flex-1 flex-col items-center justify-center px-8 py-12 ${
          photo ? "auth-form-panel" : ""
        }`}
      >
        <div className="w-full max-w-sm">
          <Link href="/" className="text-sm text-ink-muted transition-colors hover:text-accent">
            ← Volver a la tienda
          </Link>
          <h1 className="mt-2 mb-6 font-display text-2xl font-semibold text-ink">{title}</h1>
          {children}
          <div className="mt-6 text-sm text-ink-muted">{footer}</div>
        </div>
      </div>
    </div>
  );
}
