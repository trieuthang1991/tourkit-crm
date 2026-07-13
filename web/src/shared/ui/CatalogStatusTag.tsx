import { Tag } from 'antd';

/// Nhãn trạng thái danh mục bám hệ cũ (Thiết lập hệ thống): 1 = Hoạt động (xanh), 0 = Ngưng hoạt động (xám).
export const CATALOG_STATUS: Record<number, string> = { 1: 'Hoạt động', 0: 'Ngưng hoạt động' };

export function CatalogStatusTag({ status }: { status: number }) {
  return <Tag color={status === 1 ? 'blue' : 'default'}>{CATALOG_STATUS[status] ?? String(status)}</Tag>;
}
