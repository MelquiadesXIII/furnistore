import type { NextConfig } from "next";

function productImagesPattern(): URL {
  const apiBaseUrl = process.env.API_BASE_URL;
  if (!apiBaseUrl) {
    throw new Error("API_BASE_URL no está configurada (ver apps/web/.env.example)");
  }
  return new URL("/assets/**", apiBaseUrl);
}

const nextConfig: NextConfig = {
  experimental: {
    serverActions: { bodySizeLimit: "6mb" },
  },
  images: {
    remotePatterns: [productImagesPattern()],
    minimumCacheTTL: 2678400, // 31 días
  },
};

export default nextConfig;
