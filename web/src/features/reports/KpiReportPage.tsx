import { Card, Col, Row, Statistic, Typography } from 'antd';
import { money } from '../../shared/format';
import { useKpiSummary } from './kpiApi';

const pct = (v: number) => `${(v * 100).toFixed(1)}%`;

export function KpiReportPage() {
  const kpi = useKpiSummary();
  const d = kpi.data;

  return (
    <>
      <Typography.Title level={3}>KPI phễu kinh doanh</Typography.Title>
      <Typography.Paragraph type="secondary">
        Báo giá → chấp nhận → chuyển đơn → thu tiền.
      </Typography.Paragraph>
      <Row gutter={[16, 16]}>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Số báo giá" value={d?.quoteCount ?? 0} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Đã chấp nhận" value={d?.quoteAcceptedCount ?? 0} suffix={d ? `(${pct(d.acceptanceRate)})` : ''} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Chuyển thành đơn" value={d?.quoteConvertedCount ?? 0} suffix={d ? `(${pct(d.conversionRate)})` : ''} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Số đơn" value={d?.orderCount ?? 0} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Doanh thu" formatter={() => money(d?.totalRevenue ?? 0)} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Giá trị đơn TB" formatter={() => money(d?.avgOrderValue ?? 0)} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Đã thu" formatter={() => money(d?.totalReceived ?? 0)} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card loading={kpi.isLoading}>
            <Statistic title="Tỉ lệ thu" value={d ? pct(d.collectionRate) : '—'} />
          </Card>
        </Col>
      </Row>
    </>
  );
}
