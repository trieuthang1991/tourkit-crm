import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Download, Pencil, Plus, RotateCcw, Search, Trash2, Users } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';
import { Button, Card, DataGrid, Input, StatCard, StatRow, cn, gridCell } from '../../shared/ui2';
import type { DataGridColumn } from '../../shared/ui2';
import { CUSTOMER_TYPE_OPTIONS, customerSchema, customerTypeLabel } from './types';
import type { Customer } from './types';

/* Màn Khách hàng — bản MỚI (shadcn + SVAR DataGrid), chạy song song /customers cũ để duyệt look & UX.
   Dữ liệu/endpoint GIỮ NGUYÊN (/api/v1/customers + /stats). Không AntD, không AG Grid. */

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
const arr = (v: unknown): string[] => (Array.isArray(v) ? (v as string[]) : []);

/** Dòng lưới = Customer + số thứ tự hiển thị (SVAR cell không có rowIndex sẵn). */
type CustomerRow = Customer & { __no?: number };

/** Debounce nhỏ để search gõ tới đâu lọc tới đó mà không dội request. */
function useDebounced<T>(value: T, ms = 350): T {
  const [v, setV] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setV(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return v;
}

/** Ô ghép 2 dòng (bám CellStack hệ cũ): dòng chính đậm + dòng phụ mờ. */
function Stack({ main, sub }: { main?: string | null; sub?: string | null }) {
  return (
    <div className="leading-tight">
      <div className="truncate text-ink">{main || '—'}</div>
      {sub ? <div className="truncate text-xs text-muted">{sub}</div> : null}
    </div>
  );
}

export function CustomersGridPage() {
  const [searchRaw, setSearchRaw] = useState('');
  const q = useDebounced(searchRaw.trim());
  const [type, setType] = useState<number | ''>('');
  const [page, setPage] = useState(1);
  const size = 20;

  // Về trang 1 khi đổi bộ lọc.
  useEffect(() => setPage(1), [q, type]);

  const filterParams = useMemo(() => {
    const p: Record<string, unknown> = {};
    if (q) p.q = q;
    if (type !== '') p.customerType = type;
    return p;
  }, [q, type]);

  const list = useQuery({
    queryKey: ['customers-grid', { q, type, page, size }],
    queryFn: async () => {
      const { data } = await httpClient.get('/api/v1/customers', { params: { page, size, ...filterParams } });
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

  // Gắn số thứ tự hiển thị vào từng dòng (SVAR cell nhận `row`, không có rowIndex sẵn).
  const gridRows = useMemo(
    () => rows.map((r, i) => ({ ...r, __no: from + i })),
    [rows, from],
  );

  // Tổng TRANG hiện tại (bám tfoot page-sum của Razor: số lần mua + doanh thu).
  const pageSum = useMemo(
    () =>
      rows.reduce(
        (a, r) => ({ purchaseCount: a.purchaseCount + r.purchaseCount, revenue: a.revenue + r.revenue }),
        { purchaseCount: 0, revenue: 0 },
      ),
    [rows],
  );

  const onDelete = async (c: Customer) => {
    if (!window.confirm(`Xoá khách hàng "${c.fullName}"?`)) return;
    await httpClient.delete(`/api/v1/customers/${c.id}`);
    await list.refetch();
  };
  // Form thêm/sửa (drawer shadcn) là increment kế — nút giữ để bám layout Razor.
  const onEdit = (_c: Customer) => window.alert('Form sửa (drawer shadcn) sẽ làm ở bước kế tiếp.');

  // Xuất CSV TOÀN BỘ theo bộ lọc (server-side) — hơn hẳn export client chỉ trang hiện tại.
  const exportCsv = async () => {
    const res = await httpClient.get('/api/v1/customers/export', { params: filterParams, responseType: 'blob' });
    const url = URL.createObjectURL(res.data as Blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'khach-hang.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  // Cột bám ĐÚNG Razor Customers/Index.cshtml (bản chuẩn nghiệp vụ). Cell qua gridCell → giữ type dòng.
  const columns = useMemo<DataGridColumn[]>(
    () => [
      {
        id: '__no',
        header: '#',
        width: 56,
        cell: gridCell<CustomerRow>((row) => <span className="text-muted tabular-nums">{row.__no}</span>),
        footer: 'Tổng cộng (trang)',
      },
      {
        id: 'fullName',
        header: 'Khách hàng',
        flexgrow: 2,
        width: 210,
        cell: gridCell<CustomerRow>((row) => {
          const sub = [row.code, customerTypeLabel(row.customerType)].filter(Boolean).join(' · ');
          return (
            <div className="leading-tight">
              <div className="truncate font-medium text-ink">{row.fullName}</div>
              {sub ? <div className="truncate text-xs text-muted">{sub}</div> : null}
            </div>
          );
        }),
      },
      {
        id: 'phone',
        header: 'Liên hệ',
        width: 180,
        cell: gridCell<CustomerRow>((row) => <Stack main={row.phone} sub={row.email} />),
      },
      {
        id: 'city',
        header: 'Khu vực & nhóm',
        width: 190,
        cell: gridCell<CustomerRow>((row) => <Stack main={row.city} sub={arr(row.segments).join(' · ')} />),
      },
      {
        id: 'lastCareAt',
        header: 'CSKH gần nhất',
        flexgrow: 1.5,
        width: 190,
        cell: gridCell<CustomerRow>((row) => <Stack main={dateVi(row.lastCareAt)} sub={row.lastCareContent} />),
      },
      {
        id: 'assignedToNames',
        header: 'Phụ trách',
        width: 180,
        cell: gridCell<CustomerRow>((row) => {
          const main = arr(row.assignedToNames).join(', ');
          const sub = row.collaboratorName ? `CTV: ${row.collaboratorName}` : row.createdByName || '';
          return <Stack main={main} sub={sub} />;
        }),
      },
      {
        id: 'revenue',
        header: 'Doanh thu',
        width: 160,
        css: 'tk-right',
        cell: gridCell<CustomerRow>((row) => (
          <div className="w-full text-right leading-tight">
            <div className={cn('font-medium tabular-nums', row.revenue > 0 ? 'text-emerald-600' : 'text-ink')}>
              {money(row.revenue)}
            </div>
            <div className="text-xs text-muted tabular-nums">{num(row.purchaseCount)} lần mua</div>
          </div>
        )),
        footer: `${money(pageSum.revenue)} · ${num(pageSum.purchaseCount)} lần`,
      },
      {
        id: '__act',
        header: '',
        width: 96,
        cell: gridCell<CustomerRow>((row) => (
          <div className="flex h-full items-center justify-end gap-1">
            <button
              type="button"
              title="Sửa"
              className="flex size-7 items-center justify-center rounded-md text-body hover:bg-canvas"
              onClick={() => onEdit(row)}
            >
              <Pencil className="size-4" />
            </button>
            <button
              type="button"
              title="Xoá"
              className="flex size-7 items-center justify-center rounded-md text-red-500 hover:bg-red-50"
              onClick={() => onDelete(row)}
            >
              <Trash2 className="size-4" />
            </button>
          </div>
        )),
      },
    ],
    // pageSum đổi theo trang → cập nhật footer; onEdit/onDelete ổn định trong render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [pageSum],
  );

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
        <DataGrid rows={gridRows} columns={columns} loading={list.isLoading} footer height={600} />
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
