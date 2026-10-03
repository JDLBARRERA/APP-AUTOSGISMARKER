import { Suspense } from "react";
import { ProjectorScreen } from "@/components/ProjectorScreen";

export default function ProjectorPage() {
  return (
    <Suspense fallback={<main className="min-h-dvh bg-black" />}>
      <ProjectorScreen />
    </Suspense>
  );
}
