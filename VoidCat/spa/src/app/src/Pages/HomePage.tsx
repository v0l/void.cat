import { useSelector } from "react-redux";

import { Dropzone } from "@/Components/FileUpload/Dropzone";
import { GlobalStats } from "@/Components/HomePage/GlobalStats";
import { FooterLinks } from "@/Components/HomePage/FooterLinks";
import { MetricsGraph } from "@/Components/HomePage/MetricsGraph";

import { RootState } from "@/Store";

export function HomePage() {
  const metrics = useSelector((s: RootState) => s.info.info);
  return (
    <div className="page">
      <Dropzone />
      <GlobalStats />
      <MetricsGraph metrics={metrics?.timeSeriesMetrics} />
      <FooterLinks />
    </div>
  );
}
