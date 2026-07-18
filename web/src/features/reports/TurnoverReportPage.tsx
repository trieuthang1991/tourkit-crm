import { Table } from '../../shared/ui/antd';
import { Button, DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { useNavigate } from 'react-router-dom';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useTurnover } from './turnoverApi';
import type { TurnoverRow } from './turnoverApi';

export function TurnoverReportPage() {
  const navigate = useNavigate();
  const report = useTurnover();

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-doanh-thu.csv',
      ['Mã đơn', 'Doanh thu', 'Chi phí', 'Lợi nhuận'],
      (report.data ?? []).map((r) => [r.orderCode, r.revenue, r.cost, r.profit]),
    );

  const columns: ColumnsType<TurnoverRow> = [
    {
      title: 'Mã đơn',
      dataIndex: 'orderCode',
      key: 'orderCode',
      render: (v: string, row) => (
        <Button variant="text" style={{ padding: 0 }} onClick={() => navigate(`/orders/${row.orderId}`)}>
          {v}
        </Button>
      ),
    },
    { title: 'Doanh thu', dataIndex: 'revenue', key: 'revenue', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo doanh thu – lợi nhuận"
        extra={<ExportButton filename="bao-cao-doanh-thu.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
      <Table
        rowKey="orderId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
      </DataCard>
    </>
  );
}
