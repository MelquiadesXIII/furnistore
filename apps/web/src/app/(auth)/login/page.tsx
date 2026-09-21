import Link from "next/link";
import { AuthShell } from "@/modules/auth/auth-shell";
import { LoginContainer } from "@/modules/auth/login/login-container";
import { safeNextPath } from "@/modules/auth/safe-next-path";
import loginHero from "@/assets/login-hero.jpg";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string | string[] }>;
}) {
  const { next } = await searchParams;

  return (
    <AuthShell
      title="Iniciar sesión"
      photo={loginHero}
      footer={
        <>
          ¿No tienes cuenta?{" "}
          <Link href="/register" className="font-medium text-accent hover:underline">
            Crear cuenta
          </Link>
        </>
      }
    >
      <LoginContainer next={safeNextPath(next)} />
    </AuthShell>
  );
}
