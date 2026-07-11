import { Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { money } from '../../shared/format';
import { useTurnoverByDepartment } from './turnoverByDepartmentApi';
import type { TurnoverByDepartmentRow } from './turnoverByDepartmentApi';

export function TurnoverByDepartmentReportPage() {
  const report = useTurnoverByDepartment();

  const columns: ColumnsType<TurnoverByDepartmentRow> = [
    { title: 'Phòng ban', dataIndex: 'departmentName', key: 'departmentName' },
    { title: 'Số đơn', dataIndex: 'orderCount', key: 'orderCount' },
    { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
  ];

  return (
    <>
      <Typography.Title level={3}>Báo cáo doanh thu theo phòng ban</Typography.Title>
      <Table
        rowKey={(r) => r.departmentId ?? 'unassigned'}
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
    </>
  );
}
