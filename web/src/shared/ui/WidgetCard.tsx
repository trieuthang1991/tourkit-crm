import type { ReactNode } from 'react';
import { Card, List, Space } from './antd';

/**
 * WidgetCard — khối card có tiêu đề (icon + title) + hành động phụ (extra), thân cuộn.
 * Chuẩn hoá pattern "Card tiêu đề + nội dung cuộn" lặp nhiều lần trên dashboard.
 * Chỉ trình bày — nhận nội dung qua children; KHÔNG chứa logic dữ liệu.
 */
export function WidgetCard({
  title,
  icon,
  extra,
  height,
  bodyPadding = 0,
  children,
}: {
  title: ReactNode;
  icon?: ReactNode;
  extra?: ReactNode;
  height?: number | string;
  bodyPadding?: number;
  children: ReactNode;
}) {
  return (
    <Card
      title={icon ? <Space size={8}>{icon}{title}</Space> : title}
      extra={extra}
      styles={{ body: { padding: bodyPadding, height, overflow: height ? 'auto' : undefined } }}
    >
      {children}
    </Card>
  );
}

/**
 * ListWidget — WidgetCard + AntD List. Chuẩn hoá "danh sách trong card" (thông báo,
 * công nợ, bài viết, phiếu chờ...). Truyền items + renderItem; giữ nguyên nội dung dòng.
 */
export function ListWidget<T>({
  title,
  icon,
  extra,
  height = 340,
  size,
  items,
  loading,
  emptyText = 'Không có dữ liệu',
  renderItem,
}: {
  title: ReactNode;
  icon?: ReactNode;
  extra?: ReactNode;
  height?: number | string;
  size?: 'small' | 'default' | 'large';
  items: T[];
  loading?: boolean;
  emptyText?: string;
  renderItem: (item: T, index: number) => ReactNode;
}) {
  return (
    <WidgetCard title={title} icon={icon} extra={extra} height={height}>
      <List<T>
        dataSource={items}
        loading={loading}
        size={size}
        locale={{ emptyText }}
        renderItem={renderItem}
      />
    </WidgetCard>
  );
}
