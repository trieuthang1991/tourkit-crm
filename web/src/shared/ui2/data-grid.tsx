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

// Theme (Theming API v36 — KHÔNG import CSS) bám brand #eb5324 + token Vuexy của dự án.
export const tkGridTheme = themeQuartz.withParams({
  accentColor: '#eb5324',
  fontFamily: 'inherit',
  foregroundColor: '#5e5873',
  headerTextColor: '#6e6b7b',
  headerBackgroundColor: '#f6f7fb',
  headerFontWeight: 600,
  borderColor: '#eef0f5',
  rowBorder: { style: 'solid', width: 1, color: '#f2f2f6' },
  wrapperBorderRadius: 12,
  wrapperBorder: { style: 'solid', width: 1, color: '#eef0f5' },
  oddRowBackgroundColor: '#fbfbfd',
  headerHeight: 44,
  rowHeight: 54,
  cellHorizontalPadding: 16,
});

const defaultColDef: ColDef = { sortable: true, resizable: true, filter: false };

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
