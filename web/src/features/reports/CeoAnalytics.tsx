import { Button, Card, Col, Row, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { useDashboard } from './dashboardApi';
import { useCashFlow } from './cashFlowApi';
import { DepartureCalendar } from '../booking/DepartureCalendar';
import { customerCareSchema } from '../care/customerCareTypes';
import { TaskDonut } from '../workspace/TaskDonut';

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

function SectionTitle({ children }: { children: ReactNode }) {
  return (
    <Typography.Title level={5} style={{ marginTop: 20, marginBottom: 8 }}>
      {children}
    </Typography.Title>
  );
}

function KpiCard({
  title,
  value,
  isMoney = true,
  color,
  to,
}: {
  title: string;
  value: number | string;
  isMoney?: boolean;
  color?: string;
  to?: string;
}) {
  const navigate = useNavigate();
  return (
    <Col xs={12} sm={12} lg={6}>
      <Card styles={{ body: { padding: 16 } }} style={color ? { borderTop: `3px solid ${color}` } : undefined}>
        <Statistic
          title={title}
          value={value}
          valueStyle={color ? { color } : undefined}
          formatter={isMoney && typeof value === 'number' ? (v) => money(Number(v)) : undefined}
        />
        {to && (
          <Typography.Link style={{ fontSize: 12 }} onClick={() => navigate(to)}>
            Xem chi tiết ›
          </Typography.Link>
        )}
      </Card>
    </Col>
  );
}

// Bar ngang đôi (thu/chi) cho dòng tiền — tự vẽ bằng div, không cần thư viện chart.
function CashFlowBars({ rows }: { rows: { paymentMethod: string; inflow: number; outflow: number; net: number }[] }) {
  const max = Math.max(1, ...rows.flatMap((r) => [r.inflow, r.outflow]));
  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      {rows.length === 0 && <Typography.Text type="secondary">Chưa có dòng tiền</Typography.Text>}
      {rows.map((r) => (
        <div key={r.paymentMethod}>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
            <strong>{r.paymentMethod}</strong>
            <span style={{ color: r.net < 0 ? '#cf1322' : '#3f8600' }}>Ròng: {money(r.net)}</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
            <span style={{ width: 44, fontSize: 12, color: '#8c8c8c' }}>Thu</span>
            <div style={{ flex: 1, background: '#f0f0f0', borderRadius: 4, height: 14 }}>
              <div style={{ width: `${(r.inflow / max) * 100}%`, background: '#52c41a', height: 14, borderRadius: 4 }} />
            </div>
            <span style={{ width: 110, textAlign: 'right', fontSize: 12 }}>{money(r.inflow)}</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
            <span style={{ width: 44, fontSize: 12, color: '#8c8c8c' }}>Chi</span>
            <div style={{ flex: 1, background: '#f0f0f0', borderRadius: 4, height: 14 }}>
              <div style={{ width: `${(r.outflow / max) * 100}%`, background: '#ff4d4f', height: 14, borderRadius: 4 }} />
            </div>
            <span style={{ width: 110, textAlign: 'right', fontSize: 12 }}>{money(r.outflow)}</span>
          </div>
        </div>
      ))}
    </Space>
  );
}

// Phễu bán hàng — các thanh giảm dần, tự vẽ.
function FunnelBars({ stages }: { stages: { label: string; value: number; color: string }[] }) {
  const max = Math.max(1, ...stages.map((s) => s.value));
  return (
    <Space direction="vertical" size={8} style={{ width: '100%' }}>
      {stages.map((s) => (
        <div key={s.label}>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
            <span>{s.label}</span>
            <strong>{s.value}</strong>
          </div>
          <div style={{ background: '#f0f0f0', borderRadius: 4, height: 18, marginTop: 2 }}>
            <div
              style={{
                width: `${Math.max(4, (s.value / max) * 100)}%`,
                background: s.color,
                height: 18,
                borderRadius: 4,
                transition: 'width .3s',
              }}
            />
          </div>
        </div>
      ))}
    </Space>
  );
}

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
    { label: 'Đã chốt', value: os?.confirmed ?? 0, color: '#52c41a' },
    { label: 'Nháp', value: os?.draft ?? 0, color: '#8c8c8c' },
    { label: 'Huỷ', value: os?.cancelled ?? 0, color: '#f5222d' },
  ];
  const funnelStages = [
    { label: 'Báo giá', value: k?.quoteCount ?? 0, color: '#1677ff' },
    { label: 'Chấp nhận', value: Math.round((k?.quoteCount ?? 0) * (k?.acceptanceRate ?? 0)), color: '#13c2c2' },
    { label: 'Chuyển đơn', value: Math.round((k?.quoteCount ?? 0) * (k?.conversionRate ?? 0)), color: '#faad14' },
    { label: 'Đơn chốt', value: k?.orderCount ?? 0, color: '#52c41a' },
  ];

  const branchColumns: ColumnsType<z.infer<typeof branchRowSchema>> = [
    { title: 'Chi nhánh', dataIndex: 'branchName', key: 'branchName' },
    { title: 'Số đơn', dataIndex: 'orderCount', key: 'orderCount', align: 'right' },
    { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', align: 'right', render: (v: number) => money(v) },
    { title: 'Thực thu', dataIndex: 'received', key: 'received', align: 'right', render: (v: number) => money(v) },
    {
      title: 'Còn thiếu',
      dataIndex: 'outstanding',
      key: 'outstanding',
      align: 'right',
      render: (v: number) => <span style={{ color: v > 0 ? '#cf1322' : undefined }}>{money(v)}</span>,
    },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', align: 'right', render: (v: number) => money(v) },
  ];

  return (
    <div style={{ paddingBottom: 24 }}>
      <Typography.Title level={4} style={{ marginBottom: 0 }}>
        CEO Analytics
      </Typography.Title>
      <Typography.Text type="secondary">Dữ liệu kinh doanh thời gian thực</Typography.Text>

      {/* Thao tác nhanh */}
      <Card size="small" style={{ marginTop: 12 }}>
        <Space wrap size={8}>
          {[
            { label: 'Tạo đơn', to: '/orders' },
            { label: 'Tạo tour / LKH', to: '/departures' },
            { label: 'Tạo báo giá', to: '/quotes' },
            { label: 'Tạo Data khách', to: '/customers' },
            { label: 'Tạo cơ hội', to: '/leads' },
            { label: 'Tạo công việc', to: '/work-tasks' },
          ].map((a) => (
            <Button key={a.to} onClick={() => navigate(a.to)}>
              {a.label}
            </Button>
          ))}
        </Space>
      </Card>

      {/* Nhóm 1 — Doanh thu & Cơ hội */}
      <SectionTitle>Doanh thu &amp; Cơ hội</SectionTitle>
      <Row gutter={[12, 12]}>
        <KpiCard title="Tổng doanh thu" value={s?.totalRevenue ?? 0} color="#3f8600" to="/reports/turnover" />
        <KpiCard title="Doanh thu thực tế" value={s?.totalReceived ?? 0} color="#1677ff" to="/reports/cash-flow" />
        <KpiCard title="Phải thu khách hàng" value={s?.receivableOutstanding ?? 0} color="#cf1322" to="/reports/order-debt" />
        <KpiCard title="Số đơn" value={s?.orderCount ?? 0} isMoney={false} color="#722ed1" to="/orders" />
      </Row>

      {/* Nhóm 2 — Chi phí & Công nợ */}
      <SectionTitle>Chi phí &amp; Công nợ</SectionTitle>
      <Row gutter={[12, 12]}>
        <KpiCard title="Tổng chi" value={s?.totalCost ?? 0} color="#3f8600" />
        <KpiCard title="Tổng chi thực tế" value={s?.totalPaid ?? 0} color="#1677ff" />
        <KpiCard title="Công nợ NCC" value={s?.payableOutstanding ?? 0} color="#cf1322" to="/reports/provider-debt" />
        <KpiCard title="Lợi nhuận gộp" value={s?.grossProfit ?? 0} color="#722ed1" />
      </Row>

      {/* Nhóm 3 — Lợi nhuận & Hiệu quả */}
      <SectionTitle>Lợi nhuận &amp; Hiệu quả</SectionTitle>
      <Row gutter={[12, 12]}>
        <KpiCard title="Lợi nhuận thực tế" value={actualProfit} color="#3f8600" />
        <KpiCard title="Tiền hoa hồng" value={commissionTotal} color="#eb2f96" to="/reports/commission-by-user" />
        <KpiCard title="Tỉ lệ thu tiền" value={k ? pct(k.collectionRate) : '—'} isMoney={false} color="#1677ff" />
        <KpiCard title="Giá trị TB / đơn" value={k?.avgOrderValue ?? 0} color="#722ed1" />
      </Row>

      {/* Dòng tiền + (Marketing: deferred) */}
      <Row gutter={[16, 16]} style={{ marginTop: 8 }}>
        <Col xs={24} lg={16}>
          <Card title="Dòng tiền theo phương thức" size="small">
            <CashFlowBars rows={cashFlow.data ?? []} />
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="Hiệu quả Marketing" size="small">
            <Typography.Text type="secondary">Chưa có model Marketing — bổ sung sau.</Typography.Text>
          </Card>
        </Col>
      </Row>

      {/* Điều hành khởi hành (lịch) — dùng chung component DepartureCalendar với Bàn làm việc */}
      <SectionTitle>Điều hành khởi hành</SectionTitle>
      <Card size="small">
        <DepartureCalendar fullscreen={false} />
      </Card>

      {/* Trạng thái hợp đồng (donut + cards) + Phễu bán hàng */}
      <Row gutter={[16, 16]} style={{ marginTop: 8 }}>
        <Col xs={24} lg={12}>
          <Card title="Trạng thái hợp đồng" size="small">
            <Row align="middle" gutter={16}>
              <Col flex="140px" style={{ textAlign: 'center' }}>
                <TaskDonut segments={contractSegments} centerLabel="đơn" />
              </Col>
              <Col flex="auto">
                <Row gutter={[8, 8]}>
                  <Col span={12}><Statistic title="Đã chốt" value={os?.confirmed ?? 0} valueStyle={{ color: '#52c41a' }} /></Col>
                  <Col span={12}><Statistic title="Nháp" value={os?.draft ?? 0} /></Col>
                  <Col span={12}><Statistic title="Huỷ" value={os?.cancelled ?? 0} valueStyle={{ color: '#f5222d' }} /></Col>
                  <Col span={12}><Statistic title="Tổng đơn" value={os?.total ?? 0} /></Col>
                </Row>
              </Col>
            </Row>
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Phễu bán hàng thông minh" size="small">
            <FunnelBars stages={funnelStages} />
          </Card>
        </Col>
      </Row>

      {/* Quản lý lịch hẹn / chăm sóc */}
      <SectionTitle>Quản lý lịch hẹn</SectionTitle>
      <Card size="small">
        <Table
          rowKey="id"
          size="small"
          pagination={{ pageSize: 5 }}
          loading={cares.isLoading}
          dataSource={cares.data ?? []}
          columns={[
            { title: 'Tiêu đề', dataIndex: 'title', key: 'title' },
            { title: 'Nội dung', dataIndex: 'detail', key: 'detail', render: (v: string | null) => v ?? '—' },
            {
              title: 'Nhắc lúc',
              dataIndex: 'remindAt',
              key: 'remindAt',
              render: (v: string | null) => (v ? new Date(v).toLocaleString('vi-VN') : '—'),
            },
            {
              title: 'Trạng thái',
              dataIndex: 'status',
              key: 'status',
              render: (v: number) => <Tag color={CARE_STATUS[v]?.color}>{CARE_STATUS[v]?.label ?? v}</Tag>,
            },
          ]}
        />
      </Card>

      {/* Báo cáo tài chính sâu — hiệu suất theo chi nhánh */}
      <SectionTitle>Hiệu suất theo chi nhánh</SectionTitle>
      <Card size="small">
        <Table
          rowKey={(r) => r.branchId ?? 'unassigned'}
          size="small"
          columns={branchColumns}
          dataSource={branches.data ?? []}
          loading={branches.isLoading}
          pagination={false}
          scroll={{ x: 'max-content' }}
        />
      </Card>

      {/* Top sales + Top khách hàng */}
      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={12}>
          <Card title="Vinh danh chiến binh sales" size="small">
            <Table
              rowKey="userId"
              size="small"
              pagination={false}
              columns={[
                { title: 'Nhân viên', dataIndex: 'userId', key: 'userId', render: (id: string) => userName.get(id) ?? '(đã xoá)' },
                { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', align: 'right', render: (v: number) => money(v) },
                { title: 'Hoa hồng', dataIndex: 'commissionAmount', key: 'commissionAmount', align: 'right', render: (v: number) => money(v) },
              ]}
              dataSource={topSales}
              loading={commission.isLoading || users.isLoading}
            />
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Top khách hàng trung thành" size="small">
            <Table
              rowKey="customerId"
              size="small"
              pagination={false}
              columns={[
                { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName' },
                { title: 'Doanh thu', dataIndex: 'revenue', key: 'revenue', align: 'right', render: (v: number) => money(v) },
                { title: 'Đã thu', dataIndex: 'received', key: 'received', align: 'right', render: (v: number) => money(v) },
              ]}
              dataSource={topCustomers.data ?? []}
              loading={topCustomers.isLoading}
            />
          </Card>
        </Col>
      </Row>

      <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 12 }}>
        (Doanh thu cơ hội · Chi phí quản lý · Lợi nhuận ròng · Hiệu quả Marketing · Cơ cấu dịch vụ · Doanh số theo dòng sản phẩm · Phân tích thị trường địa lý: chưa có quan hệ trong model — bổ sung sau)
      </Typography.Text>
    </div>
  );
}
