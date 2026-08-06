import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { ColDef } from 'ag-grid-community';
import { Download, Pencil, Plus, RotateCcw, Search, Trash2, Users } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';
import { Button, Card, DataGrid, Input, StatCard, StatRow, cn } from '../../shared/ui2';
import { CUSTOMER_TYPE_OPTIONS, customerSchema, customerTypeLabel } from './types';
import type { Customer } from './types';
import type { GridApi, GridReadyEvent } from 'ag-grid-community';

/* Màn Khách hàng — bản MỚI (shadcn + AG Grid), chạy song song trang /customers cũ để duyệt look & UX.
   Dữ liệu/endpoint GIỮ NGUYÊN (/api/v1/customers + /stats). Không AntD. */

const pagedCustomers = z.object({
  items: z.array(customerSchema),
  total: z.number(),
  page: z.number(),
  size: z.number(),
});
const statsSchema = z.object({
  total: z.number(),
  newToday: z.number(),
  newThisMonth: z.number(),
  firstTimeBuyers: z.number(),
  repeatBuyers: z.number(),
});

const dateVi = (v: string | null | undefined) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');
const num = (n: number) => n.toLocaleString('vi-VN');

/** Debounce nhỏ để search gõ tới đâu lọc tới đó mà không dội request. */
function useDebounced<T>(value: T, ms = 350): T {
  const [v, setV] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setV(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return v;
}

export function CustomersGridPage() {
  const [searchRaw, setSearchRaw] = useState('');
  const q = useDebounced(searchRaw.trim());
  const [type, setType] = useState<number | ''>('');
  const [page, setPage] = useState(1);
  const size = 20;
  const gridApi = useRef<GridApi<Customer> | null>(null);

  // Về trang 1 khi đổi bộ lọc.
  useEffect(() => setPage(1), [q, type]);

  const list = useQuery({
    queryKey: ['customers-grid', { q, type, page, size }],
    queryFn: async () => {
      const params: Record<string, unknown> = { page, size };
      if (q) params.q = q;
      if (type !== '') params.customerType = type;
      const { data } = await httpClient.get('/api/v1/customers', { params });
      return pagedCustomers.parse(data);
    },
    placeholderData: keepPreviousData, // giữ dữ liệu cũ khi chuyển trang/lọc → không nháy trắng
  });

  const stats = useQuery({
    queryKey: ['customers-grid-stats'],
    queryFn: async () => {
      const { data } = await httpClient.get('/api/v1/customers/stats');
      return statsSchema.parse(data);
    },
  });

  const rows = list.data?.items ?? [];
  const total = list.data?.total ?? 0;
  const from = total === 0 ? 0 : (page - 1) * size + 1;
  const to = Math.min(page * size, total);
  const lastPage = Math.max(1, Math.ceil(total / size));

  const onDelete = async (c: Customer) => {
    if (!window.confirm(`Xoá khách hàng "${c.fullName}"?`)) return;
    await httpClient.delete(`/api/v1/customers/${c.id}`);
    await list.refetch();
  };
  // Form thêm/sửa (drawer shadcn) là increment kế — nút giữ để bám layout Razor.
  const onEdit = (_c: Customer) => window.alert('Form sửa (drawer shadcn) sẽ làm ở bước kế tiếp.');

  // Dòng tổng ghim đáy — tổng TRANG hiện tại (bám tfoot page-sum của Razor: số lần mua + doanh thu).
  const pinnedBottom = useMemo<Customer[]>(() => {
    if (rows.length === 0) return [];
    const sum = rows.reduce(
      (a, r) => ({ purchaseCount: a.purchaseCount + r.purchaseCount, revenue: a.revenue + r.revenue }),
      { purchaseCount: 0, revenue: 0 },
    );
    return [
      {
        ...rows[0],
        id: '__sum__',
        code: '',
        fullName: 'Tổng cộng (trang)',
        phone: null,
        email: null,
        city: null,
        segments: [],
        lastCareAt: null,
        lastCareContent: null,
        assignedToNames: [],
        collaboratorName: null,
        createdByName: null,
        ...sum,
      } as Customer,
    ];
  }, [rows]);

  const isSum = (d?: Customer) => d?.id === '__sum__';
  // Ô ghép 2 dòng (bám CellStack hệ cũ): dòng chính + dòng phụ mờ.
  const stack = (main?: string | null, sub?: string | null) => (
    <div className="leading-tight">
      <div className="truncate text-ink">{main || '—'}</div>
      {sub ? <div className="truncate text-xs text-muted">{sub}</div> : null}
    </div>
  );

  // Cột bám ĐÚNG Razor Customers/Index.cshtml (bản chuẩn nghiệp vụ).
  const columns = useMemo<ColDef<Customer>[]>(
    () => [
      {
        headerName: '#',
        valueGetter: (p) => (p.node?.rowPinned ? '' : (p.node!.rowIndex ?? 0) + from),
        width: 56,
        cellClass: 'text-muted',
        sortable: false,
      },
      {
        headerName: 'Khách hàng',
        field: 'fullName',
        flex: 2,
        minWidth: 210,
        cellRenderer: (p: { data?: Customer }) => {
          const d = p.data;
          if (isSum(d)) return <div className="font-semibold text-ink">{d?.fullName}</div>;
          const sub = [d?.code, customerTypeLabel(d!.customerType)].filter(Boolean).join(' · ');
          return (
            <div className="leading-tight">
              <div className="truncate font-medium text-ink">{d?.fullName}</div>
              {sub ? <div className="truncate text-xs text-muted">{sub}</div> : null}
            </div>
          );
        },
      },
      {
        headerName: 'Liên hệ',
        field: 'phone',
        width: 180,
        cellRenderer: (p: { data?: Customer }) => (isSum(p.data) ? null : stack(p.data?.phone, p.data?.email)),
      },
      {
        headerName: 'Khu vực & nhóm',
        field: 'city',
        width: 190,
        cellRenderer: (p: { data?: Customer }) =>
          isSum(p.data) ? null : stack(p.data?.city, (p.data?.segments ?? []).join(' · ')),
      },
      {
        headerName: 'CSKH gần nhất',
        field: 'lastCareAt',
        flex: 1.5,
        minWidth: 190,
        cellRenderer: (p: { data?: Customer }) =>
          isSum(p.data) ? null : stack(dateVi(p.data?.lastCareAt), p.data?.lastCareContent),
      },
      {
        headerName: 'Phụ trách',
        field: 'assignedToNames',
        width: 180,
        cellRenderer: (p: { data?: Customer }) => {
          const d = p.data;
          if (isSum(d)) return null;
          const main = (d?.assignedToNames ?? []).join(', ');
          const sub = d?.collaboratorName ? `CTV: ${d.collaboratorName}` : d?.createdByName || '';
          return stack(main, sub);
        },
      },
      {
        headerName: 'Doanh thu',
        field: 'revenue',
        width: 150,
        type: 'rightAligned',
        cellRenderer: (p: { data?: Customer }) => {
          const d = p.data;
          return (
            <div className="leading-tight">
              <div className={cn('font-medium tabular-nums', (d?.revenue ?? 0) > 0 ? 'text-emerald-600' : 'text-ink')}>
                {money(d?.revenue ?? 0)}
              </div>
              <div className="text-xs text-muted tabular-nums">{num(d?.purchaseCount ?? 0)} lần mua</div>
            </div>
          );
        },
      },
      {
        headerName: '',
        colId: 'actions',
        width: 96,
        sortable: false,
        type: 'rightAligned',
        cellRenderer: (p: { data?: Customer }) => {
          const d = p.data;
          if (isSum(d)) return null;
          return (
            <div className="flex h-full items-center justify-end gap-1">
              <button
                type="button"
                title="Sửa"
                className="flex size-7 items-center justify-center rounded-md text-body hover:bg-canvas"
                onClick={() => onEdit(d!)}
              >
                <Pencil className="size-4" />
              </button>
              <button
                type="button"
                title="Xoá"
                className="flex size-7 items-center justify-center rounded-md text-red-500 hover:bg-red-50"
                onClick={() => onDelete(d!)}
              >
                <Trash2 className="size-4" />
              </button>
            </div>
          );
        },
      },
    ],
    [from],
  );

  const onGridReady = (e: GridReadyEvent<Customer>) => {
    gridApi.current = e.api;
  };
  const exportCsv = () => gridApi.current?.exportDataAsCsv({ fileName: 'khach-hang.csv' });

  return (
    <div>
      {/* Header */}
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="text-xs text-muted">CRM / Data khách hàng</div>
          <h4 className="m-0 text-xl font-semibold text-ink">Khách hàng</h4>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={exportCsv}>
            <Download /> Xuất CSV
          </Button>
          <Button variant="primary">
            <Plus /> Thêm khách hàng
          </Button>
        </div>
      </div>

      {/* Thẻ thống kê */}
      <StatRow>
        <StatCard tone="brand" icon={<Users />} label="Tổng khách hàng" value={num(stats.data?.total ?? 0)} />
        <StatCard tone="info" label="Tạo hôm nay" value={num(stats.data?.newToday ?? 0)} />
        <StatCard tone="warning" label="Tạo trong tháng" value={num(stats.data?.newThisMonth ?? 0)} />
        <StatCard tone="success" label="Mua lần đầu" value={num(stats.data?.firstTimeBuyers ?? 0)} />
        <StatCard tone="danger" label="Mua lại nhiều lần" value={num(stats.data?.repeatBuyers ?? 0)} />
      </StatRow>

      {/* Toolbar lọc */}
      <Card className="mb-4">
        <div className="flex flex-wrap items-center gap-2 p-3">
          <div className="relative min-w-[260px] flex-1">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" />
            <Input
              className="pl-9"
              placeholder="Tìm mã / tên / SĐT / email…"
              value={searchRaw}
              onChange={(e) => setSearchRaw(e.target.value)}
            />
          </div>
          <select
            className="h-9 rounded-lg border border-line bg-surface px-3 text-sm text-ink focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand/40"
            value={type}
            onChange={(e) => setType(e.target.value === '' ? '' : Number(e.target.value))}
          >
            <option value="">Tất cả loại</option>
            {CUSTOMER_TYPE_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
          {(searchRaw || type !== '') && (
            <Button
              variant="ghost"
              onClick={() => {
                setSearchRaw('');
                setType('');
              }}
            >
              <RotateCcw /> Đặt lại
            </Button>
          )}
        </div>
      </Card>

      {/* Bảng */}
      <Card className="overflow-hidden">
        <DataGrid<Customer>
          rowData={rows}
          columnDefs={columns}
          loading={list.isLoading}
          pinnedBottomRowData={pinnedBottom}
          height={600}
          onGridReady={onGridReady}
        />
        {/* Phân trang */}
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line px-4 py-3 text-sm">
          <div className={cn('text-muted', list.isFetching && 'opacity-60')}>
            {from}–{to} / <span className="font-medium text-ink">{num(total)}</span> khách hàng
          </div>
          <div className="flex items-center gap-1">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Trước
            </Button>
            <span className="px-2 tabular-nums text-body">
              {page} / {lastPage}
            </span>
            <Button variant="outline" size="sm" disabled={page >= lastPage} onClick={() => setPage((p) => p + 1)}>
              Sau
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
