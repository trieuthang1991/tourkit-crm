import { Card, Col, Row, Statistic, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';
import { useDashboard } from '../reports/dashboardApi';

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

function KpiCard({ title, value, isMoney = true }: { title: string; value: number; isMoney?: boolean }) {
  return (
    <Col xs={12} sm={12} lg={6}>
      <Card styles={{ body: { padding: 16 } }}>
        <Statistic title={title} value={value} formatter={isMoney ? (v) => money(Number(v)) : undefined} />
      </Card>
    </Col>
  );
}

export function CeoAnalytics() {
  const dash = useDashboard();
  const commission = useReport('commission-by-user', '/api/v1/reports/commission-by-user', z.array(commissionRowSchema));
  const branches = useReport('turnover-by-branch', '/api/v1/reports/turnover-by-branch', z.array(branchRowSchema));
  const topCustomers = useReport('top-customers', '/api/v1/reports/top-customers?top=10', z.array(topCustomerSchema));
  const kpi = useReport('kpi', '/api/v1/reports/kpi', kpiSchema);
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });

  const s = dash.data;
  const userName = new Map((users.data ?? []).map((u) => [u.id, u.fullName]));
  const commissionTotal = (commission.data ?? []).reduce((a, r) => a + r.commissionAmount, 0);
  const actualProfit = (s?.totalReceived ?? 0) - (s?.totalPaid ?? 0);

  const topSales = [...(commission.data ?? [])].sort((a, b) => b.turnover - a.turnover).slice(0, 5);

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
    <div style={{ marginBottom: 24 }}>
      <Typography.Title level={4} style={{ marginBottom: 4 }}>
        CEO Analytics
      </Typography.Title>
      <Typography.Text type="secondary">Dữ liệu kinh doanh thời gian thực</Typography.Text>

      {/* Nhóm 1 — Doanh thu & Cơ hội */}
      <Typography.Title level={5} style={{ marginTop: 16 }}>
        Doanh thu &amp; Cơ hội
      </Typography.Title>
      <Row gutter={[12, 12]}>
        <KpiCard title="Tổng doanh thu" value={s?.totalRevenue ?? 0} />
        <KpiCard title="Doanh thu thực tế" value={s?.totalReceived ?? 0} />
        <KpiCard title="Phải thu khách hàng" value={s?.receivableOutstanding ?? 0} />
        <KpiCard title="Số đơn" value={s?.orderCount ?? 0} isMoney={false} />
      </Row>

      {/* Nhóm 2 — Chi phí & Công nợ */}
      <Typography.Title level={5} style={{ marginTop: 16 }}>
        Chi phí &amp; Công nợ
      </Typography.Title>
      <Row gutter={[12, 12]}>
        <KpiCard title="Tổng chi" value={s?.totalCost ?? 0} />
        <KpiCard title="Tổng chi thực tế" value={s?.totalPaid ?? 0} />
        <KpiCard title="Công nợ NCC" value={s?.payableOutstanding ?? 0} />
        <KpiCard title="Lợi nhuận gộp" value={s?.grossProfit ?? 0} />
      </Row>

      {/* Nhóm 3 — Lợi nhuận & Hiệu quả */}
      <Typography.Title level={5} style={{ marginTop: 16 }}>
        Lợi nhuận &amp; Hiệu quả
      </Typography.Title>
      <Row gutter={[12, 12]}>
        <KpiCard title="Lợi nhuận thực tế" value={actualProfit} />
        <KpiCard title="Tiền hoa hồng" value={commissionTotal} />
        <Col xs={12} sm={12} lg={6}>
          <Card styles={{ body: { padding: 16 } }}>
            <Statistic title="Tỉ lệ thu tiền" value={kpi.data ? pct(kpi.data.collectionRate) : '—'} />
          </Card>
        </Col>
        <Col xs={12} sm={12} lg={6}>
          <Card styles={{ body: { padding: 16 } }}>
            <Statistic title="Giá trị TB / đơn" value={money(kpi.data?.avgOrderValue ?? 0)} />
          </Card>
        </Col>
      </Row>

      {/* Phễu bán hàng */}
      <Typography.Title level={5} style={{ marginTop: 16 }}>
        Phễu bán hàng
      </Typography.Title>
      <Row gutter={[12, 12]}>
        <KpiCard title="Số báo giá" value={kpi.data?.quoteCount ?? 0} isMoney={false} />
        <Col xs={12} sm={12} lg={6}>
          <Card styles={{ body: { padding: 16 } }}>
            <Statistic title="Tỉ lệ chấp nhận báo giá" value={kpi.data ? pct(kpi.data.acceptanceRate) : '—'} />
          </Card>
        </Col>
        <Col xs={12} sm={12} lg={6}>
          <Card styles={{ body: { padding: 16 } }}>
            <Statistic title="Tỉ lệ chuyển đơn" value={kpi.data ? pct(kpi.data.conversionRate) : '—'} />
          </Card>
        </Col>
        <KpiCard title="Số đơn chốt" value={kpi.data?.orderCount ?? 0} isMoney={false} />
      </Row>

      {/* Hiệu suất theo chi nhánh */}
      <Typography.Title level={5} style={{ marginTop: 16 }}>
        Hiệu suất theo chi nhánh
      </Typography.Title>
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
      <Row gutter={[12, 12]} style={{ marginTop: 16 }}>
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
        (Doanh thu cơ hội / Chi phí quản lý / Lợi nhuận ròng / Marketing / Dòng sản phẩm theo thị trường: chưa có quan hệ trong model — bổ sung sau)
      </Typography.Text>
    </div>
  );
}
