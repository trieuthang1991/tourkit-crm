import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { Button, DataCard, SectionTitle, StatCard } from '../../ui/kit';
import type { Tone } from '../../ui/kit';
import { Statistic, Tag, Text } from '../../ui/primitives';
import { Table } from '../../ui/Table';
import type { Column } from '../../ui/Table';
import { useDashboard } from './dashboardApi';
import { useCashFlow } from './cashFlowApi';
import { DepartureCalendar } from '../booking/DepartureCalendar';
import { customerCareSchema } from '../care/customerCareTypes';
import { TaskDonut } from '../workspace/TaskDonut';

/* Màn "Tổng quan" (/dashboard) — hệ Refined. KHÔNG antd.
   Dữ liệu/hook/section giữ NGUYÊN như bản cũ; chỉ đổi lớp trình bày. */

// --- Schemas cho các endpoint report tái dùng ---
const commissionRowSchema = z.object({
  userId: z.string().uuid(),
  turnover: z.number(),
  profit: z.number(),
  commissionAmount: z.number(),
});
const branchRowSchema = z.object({
  branchId: z.string().uuid().nullable(),
  branchName: z.string(),
  orderCount: z.number(),
  turnover: z.number(),
  received: z.number(),
  outstanding: z.number(),
  profit: z.number(),
});
const topCustomerSchema = z.object({
  customerId: z.string().uuid(),
  customerName: z.string(),
  revenue: z.number(),
  received: z.number(),
});
const kpiSchema = z.object({
  quoteCount: z.number(),
  acceptanceRate: z.number(),
  conversionRate: z.number(),
  orderCount: z.number(),
  avgOrderValue: z.number(),
  collectionRate: z.number(),
});
const orderStatsSchema = z.object({
  total: z.number(),
  draft: z.number(),
  confirmed: z.number(),
  cancelled: z.number(),
  unpaid: z.number(),
  deposit: z.number(),
  paid: z.number(),
});
const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });

type BranchRow = z.infer<typeof branchRowSchema>;
type CommissionRow = z.infer<typeof commissionRowSchema>;
type TopCustomerRow = z.infer<typeof topCustomerSchema>;
type CareRow = z.infer<typeof customerCareSchema>;

function useReport<T>(key: string, url: string, schema: z.ZodType<T>) {
  return useQuery({
    queryKey: ['reports', key],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(url);
      return schema.parse(data);
    },
  });
}

const pct = (v: number) => `${(v * 100).toFixed(1)}%`;
const CARE_STATUS: Record<number, { label: string; color: string }> = {
  0: { label: 'Chờ xử lý', color: 'orange' },
  1: { label: 'Đã liên hệ', color: 'blue' },
  2: { label: 'Hoàn thành', color: 'green' },
  3: { label: 'Huỷ', color: 'red' },
};

function KpiCard({ title, value, isMoney = true, tone, to }: { title: string; value: number | string; isMoney?: boolean; tone?: Tone; to?: string }) {
  const navigate = useNavigate();
  return (
    <StatCard
      label={title}
      tone={tone}
      value={isMoney && typeof value === 'number' ? money(Number(value)) : value}
      linkText={to ? 'Xem chi tiết' : undefined}
      onLink={to ? () => navigate(to) : undefined}
    />
  );
}

// Bar ngang đôi (thu/chi) cho dòng tiền — tự vẽ bằng div, không cần thư viện chart.
function CashFlowBars({ rows }: { rows: { paymentMethod: string; inflow: number; outflow: number; net: number }[] }) {
  const max = Math.max(1, ...rows.flatMap((r) => [r.inflow, r.outflow]));
  if (rows.length === 0) return <Text type="secondary">Chưa có dòng tiền</Text>;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {rows.map((r) => (
        <div key={r.paymentMethod}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <span style={{ font: '600 13px var(--tk-font)', color: 'var(--tk-heading)' }}>{r.paymentMethod}</span>
            <span style={{ font: '600 12px var(--tk-font)', color: 'var(--tk-muted-2)' }}>
              Ròng{' '}
              <span style={{ fontFamily: 'var(--tk-font-mono)', color: r.net < 0 ? 'var(--tk-danger)' : 'var(--tk-success)' }}>
                {r.net >= 0 ? '+' : ''}
                {money(r.net)}
              </span>
            </span>
          </div>
          {(
            [
              ['Thu', r.inflow, 'var(--tk-success)'],
              ['Chi', r.outflow, 'var(--tk-danger)'],
            ] as const
          ).map(([label, v, color]) => (
            <div key={label} style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 5 }}>
              <span style={{ width: 30, font: '400 11.5px var(--tk-font)', color: 'var(--tk-muted)' }}>{label}</span>
              <div className="rf-bar__track" style={{ flex: 1 }}>
                <div className="rf-bar__fill" style={{ width: `${(v / max) * 100}%`, background: color }} />
              </div>
              <span style={{ width: 108, textAlign: 'right', font: '500 11.5px var(--tk-font-mono)', color: 'var(--tk-muted-2)' }}>{money(v)}</span>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}

// Phễu bán hàng — các thanh giảm dần, tự vẽ.
function FunnelBars({ stages }: { stages: { label: string; value: number; color: string }[] }) {
  const max = Math.max(1, ...stages.map((s) => s.value));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      {stages.map((s) => (
        <div key={s.label}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 5 }}>
            <span style={{ fontSize: 13, color: 'var(--tk-body)' }}>{s.label}</span>
            <span style={{ font: '700 13px var(--tk-font-mono)', color: 'var(--tk-heading)' }}>{s.value.toLocaleString('vi-VN')}</span>
          </div>
          <div className="rf-bar__track">
            <div className="rf-bar__fill" style={{ width: `${Math.max(4, (s.value / max) * 100)}%`, background: s.color }} />
          </div>
        </div>
      ))}
    </div>
  );
}

const QUICK = [
  { label: 'Tạo đơn', icon: 'add_shopping_cart', to: '/orders' },
  { label: 'Tạo tour / LKH', icon: 'flag', to: '/departures' },
  { label: 'Tạo báo giá', icon: 'description', to: '/quotes' },
  { label: 'Tạo Data khách', icon: 'person_add', to: '/customers' },
  { label: 'Tạo cơ hội', icon: 'my_location', to: '/leads' },
  { label: 'Tạo công việc', icon: 'task', to: '/work-tasks' },
];

export function CeoAnalytics() {
  const navigate = useNavigate();
  const dash = useDashboard();
  const cashFlow = useCashFlow();
  const commission = useReport('commission-by-user', '/api/v1/reports/commission-by-user', z.array(commissionRowSchema));
  const branches = useReport('turnover-by-branch', '/api/v1/reports/turnover-by-branch', z.array(branchRowSchema));
  const topCustomers = useReport('top-customers', '/api/v1/reports/top-customers?top=10', z.array(topCustomerSchema));
  const kpi = useReport('kpi', '/api/v1/reports/kpi', kpiSchema);
  const orderStats = useReport('order-stats', '/api/v1/orders/stats', orderStatsSchema);
  const cares = useQuery({
    queryKey: ['customer-cares', 'dashboard'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-cares', { params: { page: 1, size: 10 } });
      return pagedSchema(customerCareSchema).parse(data).items;
    },
  });
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });

  const s = dash.data;
  const os = orderStats.data;
  const k = kpi.data;
  const userName = new Map((users.data ?? []).map((u) => [u.id, u.fullName]));
  const commissionTotal = (commission.data ?? []).reduce((a, r) => a + r.commissionAmount, 0);
  const actualProfit = (s?.totalReceived ?? 0) - (s?.totalPaid ?? 0);
  const topSales = [...(commission.data ?? [])].sort((a, b) => b.turnover - a.turnover).slice(0, 5);

  const contractSegments = [
    { label: 'Đã chốt', value: os?.confirmed ?? 0, color: 'var(--tk-success)' },
    { label: 'Nháp', value: os?.draft ?? 0, color: 'var(--tk-muted)' },
    { label: 'Huỷ', value: os?.cancelled ?? 0, color: 'var(--tk-danger)' },
  ];
  const funnelStages = [
    { label: 'Báo giá', value: k?.quoteCount ?? 0, color: 'var(--tk-info)' },
    { label: 'Chấp nhận', value: Math.round((k?.quoteCount ?? 0) * (k?.acceptanceRate ?? 0)), color: 'var(--tk-info)' },
    { label: 'Chuyển đơn', value: Math.round((k?.quoteCount ?? 0) * (k?.conversionRate ?? 0)), color: 'var(--tk-warning)' },
    { label: 'Đơn chốt', value: k?.orderCount ?? 0, color: 'var(--tk-success)' },
  ];

  const branchColumns: Column<BranchRow>[] = [
    { key: 'branchName', title: 'Chi nhánh', dataIndex: 'branchName' },
    { key: 'orderCount', title: 'Số đơn', align: 'right', mono: true, render: (r) => r.orderCount.toLocaleString('vi-VN') },
    { key: 'turnover', title: 'Doanh thu', align: 'right', mono: true, render: (r) => money(r.turnover) },
    { key: 'received', title: 'Thực thu', align: 'right', mono: true, render: (r) => money(r.received) },
    {
      key: 'outstanding',
      title: 'Còn thiếu',
      align: 'right',
      mono: true,
      render: (r) => <span style={{ color: r.outstanding > 0 ? 'var(--tk-danger)' : undefined }}>{money(r.outstanding)}</span>,
    },
    { key: 'profit', title: 'Lợi nhuận', align: 'right', mono: true, render: (r) => money(r.profit) },
  ];

  const salesColumns: Column<CommissionRow>[] = [
    { key: 'userId', title: 'Nhân viên', render: (r) => userName.get(r.userId) ?? '(đã xoá)' },
    { key: 'turnover', title: 'Doanh thu', align: 'right', mono: true, render: (r) => money(r.turnover) },
    { key: 'commissionAmount', title: 'Hoa hồng', align: 'right', mono: true, render: (r) => money(r.commissionAmount) },
  ];

  const topCustomerColumns: Column<TopCustomerRow>[] = [
    { key: 'customerName', title: 'Khách hàng', dataIndex: 'customerName' },
    { key: 'revenue', title: 'Doanh thu', align: 'right', mono: true, render: (r) => money(r.revenue) },
    { key: 'received', title: 'Đã thu', align: 'right', mono: true, render: (r) => money(r.received) },
  ];

  const careColumns: Column<CareRow>[] = [
    { key: 'title', title: 'Tiêu đề', dataIndex: 'title' },
    { key: 'detail', title: 'Nội dung', render: (r) => r.detail ?? '—' },
    { key: 'remindAt', title: 'Nhắc lúc', mono: true, render: (r) => (r.remindAt ? new Date(r.remindAt).toLocaleString('vi-VN') : '—') },
    { key: 'status', title: 'Trạng thái', render: (r) => <Tag color={CARE_STATUS[r.status]?.color}>{CARE_STATUS[r.status]?.label ?? r.status}</Tag> },
  ];

  return (
    <div>
      <h1 className="rf-page__title">CEO Analytics</h1>
      <div className="rf-page__sub">Dữ liệu kinh doanh thời gian thực</div>

      {/* Thao tác nhanh */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 16 }}>
        {QUICK.map((a) => (
          <Button key={a.label} icon={a.icon} onClick={() => navigate(a.to)}>
            {a.label}
          </Button>
        ))}
      </div>

      {/* Nhóm 1 — Doanh thu & Cơ hội */}
      <div style={{ marginTop: 24 }}>
        <SectionTitle>Doanh thu &amp; Cơ hội</SectionTitle>
      </div>
      <div className="rf-grid rf-grid--4">
        <KpiCard title="Tổng doanh thu" value={s?.totalRevenue ?? 0} tone="success" to="/reports/turnover" />
        <KpiCard title="Doanh thu thực tế" value={s?.totalReceived ?? 0} tone="info" to="/reports/cash-flow" />
        <KpiCard title="Phải thu khách hàng" value={s?.receivableOutstanding ?? 0} tone="danger" to="/reports/order-debt" />
        <KpiCard title="Số đơn" value={s?.orderCount ?? 0} isMoney={false} tone="accent" to="/orders" />
      </div>

      {/* Nhóm 2 — Chi phí & Công nợ */}
      <div style={{ marginTop: 24 }}>
        <SectionTitle>Chi phí &amp; Công nợ</SectionTitle>
      </div>
      <div className="rf-grid rf-grid--4">
        <KpiCard title="Tổng chi" value={s?.totalCost ?? 0} tone="success" />
        <KpiCard title="Tổng chi thực tế" value={s?.totalPaid ?? 0} tone="info" />
        <KpiCard title="Công nợ NCC" value={s?.payableOutstanding ?? 0} tone="danger" to="/reports/provider-debt" />
        <KpiCard title="Lợi nhuận gộp" value={s?.grossProfit ?? 0} tone="accent" />
      </div>

      {/* Nhóm 3 — Lợi nhuận & Hiệu quả */}
      <div style={{ marginTop: 24 }}>
        <SectionTitle>Lợi nhuận &amp; Hiệu quả</SectionTitle>
      </div>
      <div className="rf-grid rf-grid--4">
        <KpiCard title="Lợi nhuận thực tế" value={actualProfit} tone="success" />
        <KpiCard title="Tiền hoa hồng" value={commissionTotal} tone="accent" to="/reports/commission-by-user" />
        <KpiCard title="Tỉ lệ thu tiền" value={k ? pct(k.collectionRate) : '—'} isMoney={false} tone="info" />
        <KpiCard title="Giá trị TB / đơn" value={k?.avgOrderValue ?? 0} tone="accent" />
      </div>

      {/* Insight — Trạng thái hợp đồng (donut) + Phễu bán hàng (đưa lên ngay sau KPI) */}
      <div className="rf-grid rf-grid--2" style={{ marginTop: 24 }}>
        <DataCard title="Trạng thái hợp đồng">
          <div style={{ display: 'flex', alignItems: 'center', gap: 20 }}>
            <div style={{ width: 140, textAlign: 'center', flexShrink: 0 }}>
              <TaskDonut segments={contractSegments} centerLabel="đơn" />
            </div>
            <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
              <Statistic title="Đã chốt" value={os?.confirmed ?? 0} valueStyle={{ color: 'var(--tk-success)' }} />
              <Statistic title="Nháp" value={os?.draft ?? 0} />
              <Statistic title="Huỷ" value={os?.cancelled ?? 0} valueStyle={{ color: 'var(--tk-danger)' }} />
              <Statistic title="Tổng đơn" value={os?.total ?? 0} />
            </div>
          </div>
        </DataCard>
        <DataCard title="Phễu bán hàng thông minh">
          <FunnelBars stages={funnelStages} />
        </DataCard>
      </div>

      {/* Dòng tiền theo phương thức (full-width, bỏ card Marketing rỗng) */}
      <div style={{ marginTop: 22 }}>
        <DataCard title="Dòng tiền theo phương thức">
          <CashFlowBars rows={cashFlow.data ?? []} />
        </DataCard>
      </div>

      {/* Quản lý lịch hẹn / chăm sóc */}
      <div style={{ marginTop: 24 }}>
        <SectionTitle>Quản lý lịch hẹn</SectionTitle>
      </div>
      <DataCard title="Quản lý lịch hẹn" bodyless>
        <Table columns={careColumns} data={cares.data ?? []} rowKey={(r) => r.id} loading={cares.isLoading} empty="Chưa có lịch hẹn" />
      </DataCard>

      {/* Báo cáo tài chính sâu — hiệu suất theo chi nhánh */}
      <div style={{ marginTop: 24 }}>
        <SectionTitle>Hiệu suất theo chi nhánh</SectionTitle>
      </div>
      <DataCard title="Hiệu suất theo chi nhánh" bodyless>
        <Table
          columns={branchColumns}
          data={branches.data ?? []}
          rowKey={(r) => r.branchId ?? 'unassigned'}
          loading={branches.isLoading}
          minWidth={820}
          empty="Chưa có dữ liệu chi nhánh"
        />
      </DataCard>

      {/* Top sales + Top khách hàng */}
      <div className="rf-grid rf-grid--2" style={{ marginTop: 22 }}>
        <DataCard title="Vinh danh chiến binh sales" bodyless>
          <Table columns={salesColumns} data={topSales} rowKey={(r) => r.userId} loading={commission.isLoading || users.isLoading} empty="Chưa có dữ liệu" />
        </DataCard>
        <DataCard title="Top khách hàng trung thành" bodyless>
          <Table columns={topCustomerColumns} data={topCustomers.data ?? []} rowKey={(r) => r.customerId} loading={topCustomers.isLoading} empty="Chưa có dữ liệu" />
        </DataCard>
      </div>

      {/* Lịch khởi hành — tham chiếu vận hành, đặt cuối */}
      <div style={{ marginTop: 22 }}>
        <DataCard title="Lịch khởi hành">
          <DepartureCalendar />
        </DataCard>
      </div>

      <Text type="secondary" size={12} style={{ display: 'block', marginTop: 16 }}>
        (Doanh thu cơ hội · Chi phí quản lý · Lợi nhuận ròng · Hiệu quả Marketing · Cơ cấu dịch vụ · Doanh số theo dòng sản phẩm · Phân tích thị trường địa lý: chưa có quan hệ trong model — bổ sung sau)
      </Text>
    </div>
  );
}
