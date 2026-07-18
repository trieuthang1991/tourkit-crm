import { Table } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useCommissionByUser } from './commissionByUserApi';
import type { CommissionByUserRow } from './commissionByUserApi';

export function CommissionByUserReportPage() {
  const report = useCommissionByUser();

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-hoa-hong.csv',
      ['ID người dùng', 'Doanh thu', 'Chi phí', 'Lợi nhuận', 'Tỉ lệ hoa hồng (%)', 'Hoa hồng'],
      (report.data ?? []).map((r) => [r.userId, r.turnover, r.cost, r.profit, r.commissionRate, r.commissionAmount]),
    );

  const columns: ColumnsType<CommissionByUserRow> = [
    { title: 'ID người dùng', dataIndex: 'userId', key: 'userId' },
    { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
    {
      title: 'Tỉ lệ hoa hồng',
      dataIndex: 'commissionRate',
      key: 'commissionRate',
      render: (v: number) => `${v}%`,
    },
    {
      title: 'Hoa hồng',
      dataIndex: 'commissionAmount',
      key: 'commissionAmount',
      render: (v: number) => money(v),
    },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo hoa hồng theo nhân viên"
        extra={<ExportButton filename="bao-cao-hoa-hong.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
      <Table
        rowKey="userId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
      </DataCard>
    </>
  );
}
