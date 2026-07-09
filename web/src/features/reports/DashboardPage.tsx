import { Card, Col, Row, Statistic, Typography } from 'antd';
import { money } from '../../shared/format';
import { useDashboard } from './dashboardApi';

export function DashboardPage() {
  const dashboard = useDashboard();
  const data = dashboard.data;

  const stats: Array<{ title: string; value: string | number }> = [
    { title: 'Số đơn', value: data?.orderCount ?? 0 },
    { title: 'Doanh thu', value: money(data?.totalRevenue ?? 0) },
    { title: 'Đã thu', value: money(data?.totalReceived ?? 0) },
    { title: 'Còn phải thu', value: money(data?.receivableOutstanding ?? 0) },
    { title: 'Chi phí', value: money(data?.totalCost ?? 0) },
    { title: 'Đã chi', value: money(data?.totalPaid ?? 0) },
    { title: 'Còn phải trả', value: money(data?.payableOutstanding ?? 0) },
    { title: 'Lợi nhuận gộp', value: money(data?.grossProfit ?? 0) },
  ];

  return (
    <>
      <Typography.Title level={3}>Tổng quan</Typography.Title>
      <Row gutter={[16, 16]}>
        {stats.map((s) => (
          <Col key={s.title} xs={24} sm={12} md={6}>
            <Card loading={dashboard.isLoading}>
              <Statistic title={s.title} value={s.value} />
            </Card>
          </Col>
        ))}
      </Row>
    </>
  );
}
