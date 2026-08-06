import { AgGridReact } from 'ag-grid-react';
import {
  AllCommunityModule,
  ModuleRegistry,
  themeQuartz,
  type ColDef,
  type GetRowIdParams,
  type GridReadyEvent,
  type RowClickedEvent,
} from 'ag-grid-community';
import * as React from 'react';

// AG Grid v33+ yêu cầu đăng ký module (1 lần). Community = miễn phí, đủ cho dự án.
ModuleRegistry.registerModules([AllCommunityModule]);

// Theme Quartz CHUẨN của AG Grid (Theming API v36 — KHÔNG import CSS), chỉ nhấn accent brand.
// Giữ bản sắc AG Grid (viền/hover/header) thay vì flatten thành bảng thường.
export const tkGridTheme = themeQuartz.withParams({
  accentColor: '#eb5324',
  fontFamily: 'inherit',
  headerFontWeight: 600,
  headerHeight: 46,
  rowHeight: 52,
  spacing: 8,
  wrapperBorderRadius: 12,
});

// Client-side filter/sort SẼ SAI với dữ liệu phân trang server (chỉ 20 dòng đang tải).
// Bản chuẩn AG Grid (Infinite Row Model + filter/sort đẩy xuống server) làm ở pass ag-mcp.
const defaultColDef: ColDef = {
  resizable: true,
  sortable: false,
  filter: false,
};

const localeText = {
  noRowsToShow: 'Không có dữ liệu',
  loadingOoo: 'Đang tải…',
};

export interface DataGridProps<T> {
  rowData: T[];
  columnDefs: ColDef<T>[];
  loading?: boolean;
  height?: number | string;
  getRowId?: (data: T) => string;
  pinnedBottomRowData?: T[];
  onRowClicked?: (data: T) => void;
  onGridReady?: (e: GridReadyEvent<T>) => void;
}

/** Bảng dữ liệu dùng chung (AG Grid Community) — thay AntD Table.
 *  Cột giàu qua cellRenderer JSX; dòng tổng qua pinnedBottomRowData; phân trang do trang ngoài quản lý. */
export function DataGrid<T>({
  rowData,
  columnDefs,
  loading,
  height = 560,
  getRowId,
  pinnedBottomRowData,
  onRowClicked,
  onGridReady,
}: DataGridProps<T>) {
  const rowId = React.useCallback(
    (p: GetRowIdParams<T>) => (getRowId ? getRowId(p.data) : String((p.data as { id?: string }).id ?? '')),
    [getRowId],
  );

  return (
    <div style={{ height, width: '100%' }}>
      <AgGridReact<T>
        theme={tkGridTheme}
        rowData={rowData}
        columnDefs={columnDefs}
        defaultColDef={defaultColDef}
        localeText={localeText}
        getRowId={rowId}
        pinnedBottomRowData={pinnedBottomRowData}
        loading={loading}
        animateRows
        suppressCellFocus
        onGridReady={onGridReady}
        onRowClicked={onRowClicked ? (e: RowClickedEvent<T>) => e.data && onRowClicked(e.data) : undefined}
      />
    </div>
  );
}
