import { Table } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useCashFlow } from './cashFlowApi';
import type { CashFlowRow } from './cashFlowApi';

export function CashFlowReportPage() {
  const report = useCashFlow();

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-dong-tien.csv',
      ['Phương thức', 'Thu vào', 'Chi ra', 'Ròng'],
      (report.data ?? []).map((r) => [r.paymentMethod, r.inflow, r.outflow, r.net]),
    );

  const columns: ColumnsType<CashFlowRow> = [
    { title: 'Phương thức', dataIndex: 'paymentMethod', key: 'paymentMethod' },
    { title: 'Thu vào', dataIndex: 'inflow', key: 'inflow', render: (v: number) => money(v) },
    { title: 'Chi ra', dataIndex: 'outflow', key: 'outflow', render: (v: number) => money(v) },
    { title: 'Ròng', dataIndex: 'net', key: 'net', render: (v: number) => money(v) },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo dòng tiền"
        extra={<ExportButton filename="bao-cao-dong-tien.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
      <Table
        rowKey="paymentMethod"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
      </DataCard>
    </>
  );
}
