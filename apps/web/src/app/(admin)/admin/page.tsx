import type { Metadata } from "next";
import { DashboardContainer } from "@/modules/admin/dashboard/dashboard-container";

export const metadata: Metadata = { title: "Resumen" };

export default function Page() {
  return <DashboardContainer />;
}
