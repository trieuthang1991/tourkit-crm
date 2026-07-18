import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money, statusText } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { ExportButton } from '../../shared/ui';
import { Button, DataCard, FilterChip, Icon, SeatTags, SegmentTabs, StatCardIcon, StatGrid } from '../../ui/kit';
import { DateRangeInput, SearchInput, Select } from '../../ui/inputs';
import { Tag as Pill, Text } from '../../ui/primitives';
import { Pagination, Table } from '../../ui/Table';
import type { Column } from '../../ui/Table';
import { ORDER_STATUS, orderSchema } from './seatTypes';
import type { Order } from './seatTypes';

/* Màn "Tất cả đơn hàng" (/orders) — hệ Refined. KHÔNG antd.
   GIỮ NGUYÊN 17 bộ lọc bám staging: 4 lọc chính hiện sẵn, còn lại trong panel "Lọc". */

const dateVi = (v?: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');
const dash = (v?: string | null) => v ?? '—';

const statsSchema = z.object({
  total: z.number(),
  totalRevenue: z.number(),
  totalPaid: z.number(),
  totalOutstanding: z.number(),
  draft: z.number(),
  confirmed: z.number(),
  cancelled: z.number(),
  unpaid: z.number(),
  deposit: z.number(),
  paid: z.number(),
  opUpcoming: z.number(),
  opRunning: z.number(),
  opDone: z.number(),
  opCancelled: z.number(),
});

const branchSchema = z.object({ id: z.string().uuid(), name: z.string() });
const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const ORDER_STATUS_OPTIONS = Object.entries(ORDER_STATUS).map(([value, label]) => ({ value: Number(value), label }));

type OrderAdv = {
  status?: number;
  salesUserId?: string;
  createdByUserId?: string;
  branchId?: string;
  departmentId?: string;
  createdFrom?: string;
  createdTo?: string;
  departureFrom?: string;
  departureTo?: string;
  tourType?: string;
  providerId?: string;
  marketTypeId?: string;
  tourGroupId?: string;
  bookingType?: number;
  commissionSettled?: boolean;
  operationalStatus?: number;
  collaboratorId?: string;
  invoiceStatus?: number;
};

// Tình trạng vận hành (legacy StatusTour).
const OPERATIONAL_STATUS_OPTIONS = [
  { value: 1, label: 'Sắp chạy' },
  { value: 2, label: 'Đang chạy' },
  { value: 3, label: 'Chưa quyết toán' },
  { value: 4, label: 'Đã quyết toán' },
  { value: 5, label: 'Xong' },
  { value: 6, label: 'Hủy' },
  { value: 7, label: 'Hủy không đi' },
];
const INVOICE_STATUS_OPTIONS = [
  { value: 0, label: 'Chưa xuất hóa đơn' },
  { value: 1, label: 'Đã xuất hóa đơn' },
  { value: 2, label: 'Đã duyệt hóa đơn' },
];

// Loại tour (legacy BookingType): FIT/GIT/LandTour/Booking/Dịch vụ/Visa/Xe.
const BOOKING_TYPE_OPTIONS = [
  { value: 0, label: 'Tour FIT' },
  { value: 1, label: 'Tour GIT' },
  { value: 2, label: 'LandTour/Combo' },
  { value: 3, label: 'Booking phòng' },
  { value: 4, label: 'Dịch vụ lẻ' },
  { value: 5, label: 'Visa' },
  { value: 6, label: 'Xe' },
];
const COMMISSION_OPTIONS = [
  { value: 'true', label: 'Đã chốt hoa hồng' },
  { value: 'false', label: 'Chưa chốt hoa hồng' },
];

// Bỏ field rỗng để không gửi param thừa.
function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function OrdersPage({ title = 'Đơn hàng' }: { title?: string } = {}) {
  const navigate = useNavigate();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [payStatus, setPayStatus] = useState<number | undefined>();
  const [draft, setDraft] = useState<OrderAdv>({});
  const [adv, setAdv] = useState<OrderAdv>({});
  const [showAdv, setShowAdv] = useState(false);

  const setD = (patch: Partial<OrderAdv>) => setDraft((d) => ({ ...d, ...patch }));
  const applyFilters = () => {
    setQ(search);
    setAdv(draft);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setPayStatus(undefined);
    setDraft({});
    setAdv({});
    setPage({ ...page, page: 1 });
  };
  // Chip lọc nhanh thị trường — áp dụng ngay (không cần bấm Tìm kiếm).
  const pickMarket = (marketTypeId?: string) => {
    setDraft((d) => ({ ...d, marketTypeId }));
    setAdv((a) => ({ ...a, marketTypeId }));
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['orders', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/orders/stats');
      return statsSchema.parse(data);
    },
  });
  const branches = useQuery({
    queryKey: ['branches'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/branches');
      return z.array(branchSchema).parse(data);
    },
  });
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
  const departments = useQuery({
    queryKey: ['departments'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/departments');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const filterOptions = useQuery({
    queryKey: ['orders', 'filter-options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/orders/filter-options');
      return z
        .object({
          tourTypes: z.array(z.string()),
          providers: z.array(z.object({ id: z.string().uuid(), name: z.string() })),
          collaborators: z.array(z.object({ id: z.string().uuid(), name: z.string() })),
        })
        .parse(data);
    },
  });
  const marketTypes = useQuery({
    queryKey: ['market-types'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/market-types');
      return z.array(z.object({ id: z.string().uuid(), name: z.string(), parentId: z.string().uuid().nullable() })).parse(data);
    },
  });
  const tourGroups = useQuery({
    queryKey: ['tour-groups'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/tour-groups');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const branchOpts = (branches.data ?? []).map((b) => ({ label: b.name, value: b.id }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));
  const deptOpts = (departments.data ?? []).map((d) => ({ label: d.name, value: d.id }));
  const tourTypeOpts = (filterOptions.data?.tourTypes ?? []).map((t) => ({ label: t, value: t }));
  const providerOpts = (filterOptions.data?.providers ?? []).map((p) => ({ label: p.name, value: p.id }));
  const collaboratorOpts = (filterOptions.data?.collaborators ?? []).map((c) => ({ label: c.name, value: c.id }));
  // Thị trường phân cấp: Select không có cây → làm phẳng, con thụt đầu dòng (giữ đủ cấp).
  const marketList = marketTypes.data ?? [];
  const marketOpts = marketList
    .filter((m) => !m.parentId)
    .flatMap((root) => [
      { label: root.name, value: root.id },
      ...marketList.filter((c) => c.parentId === root.id).map((c) => ({ label: `— ${c.name}`, value: c.id })),
    ]);
  const topMarkets = marketList.filter((m) => !m.parentId).map((m) => ({ label: m.name, value: m.id }));
  const groupOpts = (tourGroups.data ?? []).map((g) => ({ label: g.name, value: g.id }));

  const list = useQuery({
    queryKey: ['orders', 'list', page.page, page.size, q, payStatus, adv],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/orders', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, paymentStatus: payStatus, ...adv }),
      });
      return pagedSchema(orderSchema).parse(data);
    },
  });

  const rows = list.data?.items ?? [];

  // Xuất CSV trang hiện tại (phẳng hoá seat/tiền về số & text).
  const exportCsv = () =>
    exportRowsToCsv(
      'don-hang.csv',
      ['STT', 'Mã đơn', 'Khách hàng', 'Tour', 'Tổng chỗ', 'Giữ', 'Bán', 'Còn', 'Ngày đi', 'Tổng thu', 'Thực thu', 'Tổng chi', 'Thực chi', 'Lợi nhuận', 'Trạng thái'],
      rows.map((o, i) => [
        (page.page - 1) * page.size + i + 1,
        o.code,
        o.customerName ?? '',
        o.tourTitle ?? '',
        o.seatTotal ?? 0,
        o.seatHeld ?? 0,
        o.seatSold ?? 0,
        o.seatRemaining ?? 0,
        o.departureDate ? new Date(o.departureDate).toLocaleDateString('vi-VN') : '',
        o.totalRevenue,
        o.amountPaid ?? 0,
        o.totalCost,
        o.actualCost ?? 0,
        o.totalRevenue - o.totalCost,
        statusText(ORDER_STATUS, o.status),
      ]),
    );

  const columns: Column<Order>[] = [
    { key: '__stt', title: '#', width: 46, align: 'center', mono: true, render: (_r, i) => (page.page - 1) * page.size + i + 1 },
    { key: 'code', title: 'Mã đơn', width: 132, render: (o) => <span style={{ font: '600 12.5px var(--tk-font-mono)', color: 'var(--tk-accent)' }}>{o.code}</span> },
    { key: 'customerName', title: 'Khách hàng', width: 168, render: (o) => dash(o.customerName) },
    { key: 'tourTitle', title: 'Tour', width: 210, render: (o) => dash(o.tourTitle) },
    {
      key: '__seats',
      title: 'Khách (chỗ)',
      width: 150,
      render: (o) => <SeatTags total={o.seatTotal ?? 0} hold={o.seatHeld ?? 0} sold={o.seatSold ?? 0} left={o.seatRemaining ?? 0} />,
    },
    { key: 'departureDate', title: 'Ngày đi', width: 104, mono: true, render: (o) => dateVi(o.departureDate) },
    {
      key: '__thu',
      title: 'Thu tiền',
      width: 140,
      align: 'right',
      render: (o) => (
        <div style={{ lineHeight: 1.45 }}>
          <div style={{ font: '600 12.5px var(--tk-font-mono)', color: 'var(--tk-heading)' }} title="Tổng thu">
            {money(o.totalRevenue)}
          </div>
          <div style={{ font: '500 11.5px var(--tk-font-mono)', color: 'var(--tk-success)' }} title="Thực thu">
            {money(o.amountPaid ?? 0)}
          </div>
        </div>
      ),
    },
    {
      key: '__chi',
      title: 'Chi tiền',
      width: 140,
      align: 'right',
      render: (o) => (
        <div style={{ lineHeight: 1.45 }}>
          <div style={{ font: '600 12.5px var(--tk-font-mono)', color: 'var(--tk-heading)' }} title="Tổng chi">
            {money(o.totalCost)}
          </div>
          <div style={{ font: '500 11.5px var(--tk-font-mono)', color: 'var(--tk-danger)' }} title="Thực chi">
            {money(o.actualCost ?? 0)}
          </div>
        </div>
      ),
    },
    {
      key: '__loi',
      title: 'Lợi nhuận',
      width: 126,
      align: 'right',
      render: (o) => {
        const profit = o.totalRevenue - o.totalCost;
        return <span style={{ font: '700 12.5px var(--tk-font-mono)', color: profit < 0 ? 'var(--tk-danger)' : 'var(--tk-success)' }}>{money(profit)}</span>;
      },
    },
    {
      key: 'status',
      title: 'Trạng thái',
      width: 108,
      render: (o) => <Pill color={o.status === 2 ? 'green' : o.status === 3 ? 'red' : 'default'}>{statusText(ORDER_STATUS, o.status)}</Pill>,
    },
    {
      key: '__detail',
      title: '',
      width: 44,
      align: 'center',
      render: (o) => (
        <span title="Chi tiết" style={{ color: 'var(--tk-muted)', display: 'inline-flex' }} onClick={() => navigate(`/orders/${o.id}`, { state: { order: o } })}>
          <Icon name="chevron_right" size={18} />
        </span>
      ),
    },
  ];

  // Tổng cộng trang hiện tại — gộp 1 dòng tfoot (giữ đủ 6 chỉ số như bản cũ).
  const sum = (f: (o: Order) => number) => rows.reduce((a, o) => a + f(o), 0);
  const sRev = sum((o) => o.totalRevenue ?? 0);
  const sPaid = sum((o) => o.amountPaid ?? 0);
  const sCost = sum((o) => o.totalCost ?? 0);
  const sActual = sum((o) => o.actualCost ?? 0);
  const sOwe = sum((o) => o.outstanding ?? 0);
  const sProfit = sRev - sCost;
  const summary = (
    <td colSpan={columns.length}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px 22px', alignItems: 'baseline' }}>
        <strong style={{ color: 'var(--tk-heading)' }}>Tổng cộng (trang này)</strong>
        {(
          [
            ['Tổng thu', sRev, undefined],
            ['Thực thu', sPaid, 'var(--tk-success)'],
            ['Tổng chi', sCost, undefined],
            ['Thực chi', sActual, 'var(--tk-danger)'],
            ['Lợi nhuận', sProfit, sProfit < 0 ? 'var(--tk-danger)' : 'var(--tk-success)'],
            ['Phải thu', sOwe, undefined],
          ] as const
        ).map(([label, v, color]) => (
          <span key={label} style={{ color: 'var(--tk-muted-2)', fontSize: 12 }}>
            {label}: <strong style={{ fontFamily: 'var(--tk-font-mono)', color: color ?? 'var(--tk-heading)' }}>{money(v)}</strong>
          </span>
        ))}
      </div>
    </td>
  );

  const s = stats.data;
  const kpiItems = [
    { label: 'Tổng số đơn', value: (s?.total ?? 0).toLocaleString('vi-VN') },
    { label: 'Doanh thu', value: money(s?.totalRevenue ?? 0) },
    { label: 'Đã thu', value: money(s?.totalPaid ?? 0) },
    { label: 'Còn nợ', value: money(s?.totalOutstanding ?? 0) },
    { label: 'Đã chốt', value: (s?.confirmed ?? 0).toLocaleString('vi-VN') },
    { label: 'Đã huỷ', value: (s?.cancelled ?? 0).toLocaleString('vi-VN') },
  ];

  return (
    <>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
        <h1 className="rf-page__title" style={{ margin: 0 }}>{title}</h1>
        <ExportButton filename="don-hang.csv" onExport={exportCsv} />
      </div>

      {/* Thẻ thống kê (bám hệ cũ) */}
      <StatGrid items={kpiItems} min={180} style={{ marginTop: 16 }} />

      {/* Thanh lọc (bám staging /all-orders) — 4 lọc chính + panel nâng cao giữ đủ 17 bộ lọc */}
      <div className="rf-card" style={{ padding: 12, marginTop: 16 }}>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
          <SearchInput value={search} onChange={setSearch} onEnter={applyFilters} placeholder="Mã, tên tour, SĐT, tên khách…" style={{ flex: '1 1 260px' }} />
          <Select style={{ width: 150 }} allowClear placeholder="Tình trạng đơn" options={ORDER_STATUS_OPTIONS} value={draft.status} onChange={(v) => setD({ status: (v as number) ?? undefined })} />
          <Select style={{ width: 160 }} allowClear showSearch placeholder="NV phụ trách" options={userOpts} value={draft.salesUserId} onChange={(v) => setD({ salesUserId: (v as string) ?? undefined })} />
          <Select style={{ width: 150 }} allowClear showSearch placeholder="Chi nhánh" options={branchOpts} value={draft.branchId} onChange={(v) => setD({ branchId: (v as string) ?? undefined })} />
          <Button icon="tune" onClick={() => setShowAdv((v) => !v)}>
            Lọc nâng cao
          </Button>
          <Button variant="primary" icon="search" onClick={applyFilters}>
            Tìm kiếm
          </Button>
          <Button variant="text" onClick={resetFilters}>
            Đặt lại
          </Button>
        </div>

        {showAdv ? (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 10, marginTop: 12, paddingTop: 12, borderTop: '1px solid var(--tk-line)' }}>
            <DateRangeInput from={draft.createdFrom} to={draft.createdTo} placeholder={['Ngày tạo từ', 'đến']} onChange={(f, t) => setD({ createdFrom: f, createdTo: t })} />
            <DateRangeInput from={draft.departureFrom} to={draft.departureTo} placeholder={['Ngày khởi hành từ', 'đến']} onChange={(f, t) => setD({ departureFrom: f, departureTo: t })} />
            <Select allowClear showSearch placeholder="Người tạo" options={userOpts} value={draft.createdByUserId} onChange={(v) => setD({ createdByUserId: (v as string) ?? undefined })} />
            <Select allowClear showSearch placeholder="Phòng ban" options={deptOpts} value={draft.departmentId} onChange={(v) => setD({ departmentId: (v as string) ?? undefined })} />
            <Select allowClear showSearch placeholder="Loại hình" options={tourTypeOpts} value={draft.tourType} onChange={(v) => setD({ tourType: (v as string) ?? undefined })} />
            <Select allowClear showSearch placeholder="Nhà cung cấp" options={providerOpts} value={draft.providerId} onChange={(v) => setD({ providerId: (v as string) ?? undefined })} />
            <Select allowClear placeholder="Loại tour" options={BOOKING_TYPE_OPTIONS} value={draft.bookingType} onChange={(v) => setD({ bookingType: (v as number) ?? undefined })} />
            <Select allowClear showSearch placeholder="Thị trường" options={marketOpts} value={draft.marketTypeId} onChange={(v) => setD({ marketTypeId: (v as string) ?? undefined })} />
            <Select allowClear showSearch placeholder="Nhóm" options={groupOpts} value={draft.tourGroupId} onChange={(v) => setD({ tourGroupId: (v as string) ?? undefined })} />
            <Select
              allowClear
              placeholder="TT hoa hồng"
              options={COMMISSION_OPTIONS}
              value={draft.commissionSettled === undefined ? undefined : String(draft.commissionSettled)}
              onChange={(v) => setD({ commissionSettled: v == null ? undefined : v === 'true' })}
            />
            <Select allowClear placeholder="Tình trạng" options={OPERATIONAL_STATUS_OPTIONS} value={draft.operationalStatus} onChange={(v) => setD({ operationalStatus: (v as number) ?? undefined })} />
            <Select allowClear showSearch placeholder="CTV" options={collaboratorOpts} value={draft.collaboratorId} onChange={(v) => setD({ collaboratorId: (v as string) ?? undefined })} />
            <Select allowClear placeholder="TT hóa đơn" options={INVOICE_STATUS_OPTIONS} value={draft.invoiceStatus} onChange={(v) => setD({ invoiceStatus: (v as number) ?? undefined })} />
          </div>
        ) : null}
      </div>

      {/* Lọc nhanh theo thị trường (chip) — bám staging: chỉ cấp CHA, lọc gồm cả con cháu */}
      {topMarkets.length > 0 && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap', marginTop: 14 }}>
          <Text type="secondary" size={12}>
            Lọc nhanh thị trường:
          </Text>
          <FilterChip active={adv.marketTypeId === undefined} onClick={() => pickMarket(undefined)}>
            Tất cả
          </FilterChip>
          {topMarkets.map((m) => (
            <FilterChip key={m.value} active={adv.marketTypeId === m.value} onClick={() => pickMarket(m.value)}>
              {m.label}
            </FilterChip>
          ))}
        </div>
      )}

      {/* Tabs trạng thái thanh toán (bám staging: Chưa TT · Đã cọc · TT hết) */}
      <div style={{ marginTop: 14, overflowX: 'auto' }}>
        <SegmentTabs
          value={payStatus === undefined ? 'all' : String(payStatus)}
          onChange={(val) => {
            setPayStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[
            { label: 'Tất cả', value: 'all', count: s?.total ?? 0 },
            { label: 'Chưa thanh toán', value: '0', count: s?.unpaid ?? 0 },
            { label: 'Đã cọc', value: '1', count: s?.deposit ?? 0 },
            { label: 'Thanh toán hết', value: '2', count: s?.paid ?? 0 },
          ]}
        />
      </div>

      <div style={{ marginTop: 14 }}>
        <DataCard
          title="Danh sách đơn hàng / Tour"
          bodyless
          extra={
            <span style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 11.5, color: 'var(--tk-muted-2)' }}>
              Chú thích chỗ:
              <span className="rf-seat">
                <span className="rf-seat__total">Tổng</span>
                <span className="rf-seat__hold">Giữ</span>
                <span className="rf-seat__sold">Bán</span>
                <span className="rf-seat__left">Còn</span>
              </span>
            </span>
          }
        >
          <Table columns={columns} data={rows} rowKey={(o) => o.id} loading={list.isLoading} minWidth={1320} summary={rows.length ? summary : undefined} empty="Không có đơn hàng" />
          <div style={{ padding: '10px 16px' }}>
            <Pagination page={page.page} pageSize={page.size} total={list.data?.total ?? 0} unit="đơn" onChange={(p) => setPage({ ...page, page: p })} />
          </div>
        </DataCard>
      </div>

      {/* Thẻ thống kê vận hành (bám staging: Tổng tour · Đang chạy · Sắp chạy · Hoàn thành · Hủy) */}
      <div className="rf-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', marginTop: 18 }}>
        {(
          [
            { title: 'Tổng số tour', value: s?.total ?? 0, icon: 'flag', tone: 'muted' as const },
            { title: 'Đang chạy', value: s?.opRunning ?? 0, icon: 'directions_run', tone: 'info' as const },
            { title: 'Sắp chạy', value: s?.opUpcoming ?? 0, icon: 'schedule', tone: 'warning' as const },
            { title: 'Hoàn thành', value: s?.opDone ?? 0, icon: 'check_circle', tone: 'success' as const },
            { title: 'Hủy', value: s?.opCancelled ?? 0, icon: 'cancel', tone: 'danger' as const },
          ] as const
        ).map((c) => (
          <StatCardIcon key={c.title} icon={c.icon} tone={c.tone} value={c.value.toLocaleString('vi-VN')} label={c.title} />
        ))}
      </div>
    </>
  );
}
