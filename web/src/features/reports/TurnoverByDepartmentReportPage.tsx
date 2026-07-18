import { Table } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useTurnoverByDepartment } from './turnoverByDepartmentApi';
import type { TurnoverByDepartmentRow } from './turnoverByDepartmentApi';

export function TurnoverByDepartmentReportPage() {
  const report = useTurnoverByDepartment();

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-doanh-thu-phong-ban.csv',
      ['Phòng ban', 'Số đơn', 'Doanh thu', 'Chi phí', 'Lợi nhuận'],
      (report.data ?? []).map((r) => [r.departmentName, r.orderCount, r.turnover, r.cost, r.profit]),
    );

  const columns: ColumnsType<TurnoverByDepartmentRow> = [
    { title: 'Phòng ban', dataIndex: 'departmentName', key: 'departmentName' },
    { title: 'Số đơn', dataIndex: 'orderCount', key: 'orderCount' },
    { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo doanh thu theo phòng ban"
        extra={<ExportButton filename="bao-cao-doanh-thu-phong-ban.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
      <Table
        rowKey={(r) => r.departmentId ?? 'unassigned'}
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
      </DataCard>
    </>
  );
}
