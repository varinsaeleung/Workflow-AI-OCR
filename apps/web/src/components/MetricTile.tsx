import type { LucideIcon } from "lucide-react";

interface MetricTileProps {
  label: string;
  value: number;
  tone: "blue" | "teal" | "amber" | "green" | "red";
  icon: LucideIcon;
}

/**
 * Displays one dashboard metric with a stable tile size.
 */
export function MetricTile({ label, value, tone, icon: Icon }: MetricTileProps) {
  return (
    <section className={`metric-tile metric-tile--${tone}`} aria-label={label}>
      <div className="metric-tile__icon">
        <Icon size={18} aria-hidden="true" />
      </div>
      <div>
        <p className="metric-tile__label">{label}</p>
        <p className="metric-tile__value">{value}</p>
      </div>
    </section>
  );
}
