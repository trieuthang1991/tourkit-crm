import { Table } from '../../shared/ui/antd';
import { Button, DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { useNavigate } from 'react-router-dom';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useOrderDebt } from './reportApi';
import type { OrderDebtRow } from './reportApi';

export function OrderDebtReportPage() {
  const navigate = useNavigate();
  const report = useOrderDebt();

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-cong-no.csv',
      ['Mã đơn', 'Tổng tiền', 'Đã thu', 'Còn nợ'],
      (report.data ?? []).map((r) => [r.orderCode, r.total, r.paid, r.outstanding]),
    );

  const columns: ColumnsType<OrderDebtRow> = [
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
    { title: 'Tổng tiền', dataIndex: 'total', key: 'total', render: (v: number) => money(v) },
    { title: 'Đã thu', dataIndex: 'paid', key: 'paid', render: (v: number) => money(v) },
    { title: 'Còn nợ', dataIndex: 'outstanding', key: 'outstanding', render: (v: number) => money(v) },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo công nợ"
        extra={<ExportButton filename="bao-cao-cong-no.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
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
