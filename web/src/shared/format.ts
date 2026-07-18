export function money(value: number): string {
  return value.toLocaleString('vi-VN');
}

/**
 * Rút gọn tiền VND cho thẻ KPI (tránh số quá dài làm vỡ card): ≥1 tỷ → "x,y tỷ", ≥1 triệu → "x,y tr",
 * ≥1 nghìn → "x,y k", còn lại giữ nguyên. Giữ dấu âm. Dùng cho stat/KPI; bảng vẫn dùng money() đầy đủ.
 */
export function moneyCompact(value: number): string {
  const abs = Math.abs(value);
  const sign = value < 0 ? '-' : '';
  const fmt = (n: number, unit: string) => {
    const s = n.toLocaleString('vi-VN', { maximumFractionDigits: 1 });
    return `${sign}${s} ${unit}`;
  };
  if (abs >= 1_000_000_000) return fmt(abs / 1_000_000_000, 'tỷ');
  if (abs >= 1_000_000) return fmt(abs / 1_000_000, 'tr');
  if (abs >= 1_000) return fmt(abs / 1_000, 'k');
  return value.toLocaleString('vi-VN');
}

export function dateText(iso: string | null | undefined): string {
  if (!iso) {
    return '';
  }
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleString('vi-VN');
}

export function statusText(map: Record<number, string>, code: number): string {
  return map[code] ?? String(code);
}
