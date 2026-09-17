import type { NextConfig } from "next";

function supabaseHostname(): string {
  const supabaseUrl = process.env.SUPABASE_URL;
  if (!supabaseUrl) {
    throw new Error("SUPABASE_URL no está configurada (ver apps/web/.env.example)");
  }
  return new URL(supabaseUrl).hostname;
}

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: "https",
        hostname: supabaseHostname(),
        pathname: "/storage/v1/object/public/**",
      },
    ],
    minimumCacheTTL: 2678400, // 31 días
  },
};

export default nextConfig;
