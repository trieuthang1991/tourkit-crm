/* Xuất CSV phía client (không cần backend) — xuất đúng các dòng đang tải trên bảng.
   File UTF-8 kèm BOM để Excel đọc đúng tiếng Việt; escape theo RFC4180. */

export type CsvValue = string | number | null | undefined;

// Chuẩn hoá 1 ô về chuỗi an toàn: số → chuỗi thô, Date → dd/MM/yyyy, null/undefined → rỗng.
export function csvCell(v: CsvValue | boolean | Date): string {
  if (v === null || v === undefined) {
    return '';
  }
  if (typeof v === 'number') {
    return Number.isFinite(v) ? String(v) : '';
  }
  if (typeof v === 'boolean') {
    return v ? 'Có' : 'Không';
  }
  if (v instanceof Date) {
    return Number.isNaN(v.getTime()) ? '' : v.toLocaleDateString('vi-VN');
  }
  return String(v);
}

// Escape RFC4180: nếu ô chứa dấu ", phẩy hoặc xuống dòng → bọc trong ngoặc kép và nhân đôi dấu ".
function escapeCsv(s: string): string {
  return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
}

// Build CSV từ headers + rows rồi kích hoạt tải file qua Blob + thẻ <a download> tạm thời.
export function exportRowsToCsv(
  filename: string,
  headers: string[],
  rows: CsvValue[][],
): void {
  const lines = [headers, ...rows].map((row) => row.map((cell) => escapeCsv(csvCell(cell))).join(','));
  // BOM (U+FEFF) giúp Excel nhận UTF-8 → hiển thị đúng tiếng Việt.
  const csv = '﻿' + lines.join('\r\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename.toLowerCase().endsWith('.csv') ? filename : `${filename}.csv`;
  a.style.display = 'none';
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
