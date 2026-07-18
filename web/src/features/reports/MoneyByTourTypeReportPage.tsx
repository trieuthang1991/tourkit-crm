import { Table } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useMoneyByTourType } from './moneyByTourTypeApi';
import type { MoneyByTourTypeRow } from './moneyByTourTypeApi';

/* Báo cáo "Thu chi theo loại tour" (/reports/money-by-tour-type): gom đơn theo loại tour (FIT/GIT…),
   trừ hoàn/huỷ chỗ ra doanh thu ròng. Đối chiếu legacy MoneyReport (nhánh FIT + hoàn huỷ). */

export function MoneyByTourTypeReportPage() {
  const report = useMoneyByTourType();

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-thu-chi-loai-tour.csv',
      ['Loại tour', 'Số đơn', 'Doanh thu gộp', 'Hoàn/huỷ', 'Doanh thu ròng', 'Chi phí', 'Lợi nhuận'],
      (report.data ?? []).map((r) => [r.tourTypeName, r.orderCount, r.grossRevenue, r.refund, r.netRevenue, r.cost, r.profit]),
    );

  const columns: ColumnsType<MoneyByTourTypeRow> = [
    { title: 'Loại tour', dataIndex: 'tourTypeName', key: 'tourTypeName' },
    { title: 'Số đơn', dataIndex: 'orderCount', key: 'orderCount', align: 'right' },
    { title: 'Doanh thu gộp', dataIndex: 'grossRevenue', key: 'grossRevenue', align: 'right', render: (v: number) => money(v) },
    {
      title: 'Hoàn/huỷ',
      dataIndex: 'refund',
      key: 'refund',
      align: 'right',
      render: (v: number) => <span style={{ color: v > 0 ? 'var(--tk-danger)' : undefined }}>{money(v)}</span>,
    },
    { title: 'Doanh thu ròng', dataIndex: 'netRevenue', key: 'netRevenue', align: 'right', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', align: 'right', render: (v: number) => money(v) },
    {
      title: 'Lợi nhuận',
      dataIndex: 'profit',
      key: 'profit',
      align: 'right',
      render: (v: number) => <span style={{ color: v < 0 ? 'var(--tk-danger)' : 'var(--tk-success)' }}>{money(v)}</span>,
    },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo thu chi theo loại tour"
        extra={<ExportButton filename="bao-cao-thu-chi-loai-tour.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
        <Table
          rowKey="bookingType"
          columns={columns}
          dataSource={report.data ?? []}
          loading={report.isLoading}
          scroll={{ x: 'max-content' }}
          pagination={false}
        />
      </DataCard>
    </>
  );
}
