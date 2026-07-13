import { Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { ReactNode } from 'react';

// Bảng danh sách chuẩn: bọc AntD Table + cột STT (fixed trái) + cột Thao tác (fixed phải)
// + phân trang + scroll ngang. Header IN HOA & hover đã lo bằng CSS/token.
export type DataTableProps<T> = {
  rowKey: (r: T) => string;
  columns: ColumnsType<T>;
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  loading?: boolean;
  onPageChange: (page: number, size: number) => void;
  showIndex?: boolean;                    // cột STT tự sinh
  actions?: (r: T) => ReactNode;          // cột Thao tác (fixed phải)
  summary?: ReactNode;                    // dòng tổng (tuỳ chọn)
};

export function DataTable<T>({
  rowKey, columns, data, total, page, pageSize, loading, onPageChange, showIndex = true, actions, summary,
}: DataTableProps<T>) {
  const cols: ColumnsType<T> = [
    ...(showIndex
      ? [{
          title: 'STT', key: '__stt', width: 60, fixed: 'left' as const, align: 'center' as const,
          render: (_: unknown, __: T, i: number) => (page - 1) * pageSize + i + 1,
        }]
      : []),
    ...columns,
    ...(actions
      ? [{
          title: 'Thao tác', key: '__actions', width: 120, fixed: 'right' as const, align: 'center' as const,
          render: (_: unknown, r: T) => actions(r),
        }]
      : []),
  ];

  return (
    <Table<T>
      rowKey={rowKey}
      columns={cols}
      dataSource={data}
      loading={loading}
      scroll={{ x: 'max-content' }}
      pagination={{ current: page, pageSize, total, showSizeChanger: true, onChange: onPageChange }}
      summary={summary ? () => <>{summary}</> : undefined}
    />
  );
}
