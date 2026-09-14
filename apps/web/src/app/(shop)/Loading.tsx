import { ChairMark } from "@/components/furniture-marks";

export default function Loading() {
  return (
    <div className="flex flex-1 items-center justify-center py-32">
      <ChairMark className="h-12 w-12 animate-pulse text-accent" />
    </div>
  );
}