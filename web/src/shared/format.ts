export function money(value: number): string {
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
