// Donut tỉ lệ công việc thuần SVG (không cần thư viện chart) — bám widget "Tỉ lệ công việc" hệ cũ.
export type DonutSegment = { label: string; value: number; color: string };

export function TaskDonut({
  segments,
  size = 132,
  stroke = 16,
  centerLabel = 'công việc',
}: {
  segments: DonutSegment[];
  size?: number;
  stroke?: number;
  centerLabel?: string;
}) {
  const total = segments.reduce((s, x) => s + x.value, 0);
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const cx = size / 2;

  let offset = 0;
  const arcs =
    total > 0
      ? segments
          .filter((s) => s.value > 0)
          .map((s) => {
            const len = (s.value / total) * c;
            const el = (
              <circle
                key={s.label}
                cx={cx}
                cy={cx}
                r={r}
                fill="none"
                stroke={s.color}
                strokeWidth={stroke}
                strokeDasharray={`${len} ${c - len}`}
                strokeDashoffset={-offset}
                transform={`rotate(-90 ${cx} ${cx})`}
              />
            );
            offset += len;
            return el;
          })
      : [];

  return (
    <svg width={size} height={size} role="img" aria-label="Tỉ lệ công việc">
      <circle cx={cx} cy={cx} r={r} fill="none" stroke="#f0f0f0" strokeWidth={stroke} />
      {arcs}
      <text x={cx} y={cx - 4} textAnchor="middle" fontSize="22" fontWeight="700" fill="#333">
        {total}
      </text>
      <text x={cx} y={cx + 16} textAnchor="middle" fontSize="11" fill="#999">
        {centerLabel}
      </text>
    </svg>
  );
}
