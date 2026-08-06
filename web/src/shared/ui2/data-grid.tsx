import { Grid, Willow } from '@svar-ui/react-grid';
import type { IApi, IColumnConfig } from '@svar-ui/react-grid';
import type { ReactNode } from 'react';
import '@svar-ui/react-grid/all.css';
import './data-grid.css';

// Bảng dữ liệu dùng chung — SVAR React DataGrid (MIT, mã nguồn mở, không khoá tính năng như AG Grid Enterprise).
// Ô giàu (cột nhiều dòng, badge, tiền) qua `cell: FC<{row}>`; dòng tổng qua `footer` từng cột (bật `footer`).
// Virtual-scroll sẵn có → mượt với nhiều dòng. Phân trang/tìm kiếm/lọc do trang ngoài đẩy xuống server.

// Kiểu cột SVAR — trang khai báo cột theo shape này.
export type DataGridColumn = IColumnConfig;
export type DataGridApi = IApi;
// Props ô tuỳ biến (cell renderer). Dùng qua adapter `gridCell()` bên dưới để giữ type dòng.
export type { ICellProps as DataGridCellProps } from '@svar-ui/react-grid';

/** Adapter: bọc renderer nhận dòng đã ép kiểu `T` thành `cell` hợp lệ của SVAR (row: IRow rộng). */
export function gridCell<T>(render: (row: T) => ReactNode) {
  const Cell = ({ row }: { row: Record<string, unknown> }) => render(row as unknown as T);
  return Cell as IColumnConfig['cell'];
}

export interface DataGridProps {
  /** Mỗi dòng phải có `id` duy nhất (Customer.id…). */
  rows: readonly Record<string, unknown>[];
  columns: IColumnConfig[];
  loading?: boolean;
  height?: number | string;
  /** Bật hàng chân (dùng `footer` của từng cột — vd tổng trang). */
  footer?: boolean;
  /** Cho kéo đổi thứ tự cột (miễn phí ở SVAR). */
  reorder?: boolean;
  onReady?: (api: IApi) => void;
}

export function DataGrid({
  rows,
  columns,
  loading,
  height = 600,
  footer = false,
  reorder = true,
  onReady,
}: DataGridProps) {
  return (
    <div className="tk-grid" style={{ height, width: '100%' }}>
      <Willow>
        <Grid
          data={rows as Record<string, unknown>[]}
          columns={columns}
          footer={footer}
          reorder={reorder}
          overlay={loading && rows.length === 0 ? 'Đang tải…' : undefined}
          init={onReady}
        />
      </Willow>
    </div>
  );
}
