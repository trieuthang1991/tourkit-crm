// Pill trạng thái chuẩn — thay các <Tag color=...> rải rác.
// tone map sẵn cho các trạng thái nghiệp vụ hay dùng.
type Tone = 'success' | 'warning' | 'danger' | 'muted';

export function StatusTag({ tone, children }: { tone: Tone; children: React.ReactNode }) {
  return <span className={`tk-pill tk-pill--${tone}`}>{children}</span>;
}

// Ví dụ helper map từ mã trạng thái nghiệp vụ -> tone (giữ nhãn từ *_STATUS của repo):
// RECEIPT_STATUS / PAYMENT_STATUS: 0 Chờ duyệt(warning) · 1 Đã duyệt(success) · 2 Từ chối(danger)
export const voucherTone = (s: number): Tone => (s === 1 ? 'success' : s === 2 ? 'danger' : 'warning');
// INVOICE_STATUS: 0 Nháp(muted) · 1 Phát hành(success) · 2 Huỷ(danger)
export const invoiceTone = (s: number): Tone => (s === 1 ? 'success' : s === 2 ? 'danger' : 'muted');
// Provider/entity status: 1 Hoạt động(success) · 0 Ngừng(muted)
export const activeTone = (s: number): Tone => (s === 1 ? 'success' : 'muted');
