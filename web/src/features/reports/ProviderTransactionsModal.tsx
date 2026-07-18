import dayjs from 'dayjs';
import { Modal, Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { CellDate, CellMoney, CellText } from '../../shared/ui/TableCells';
import { useProviderTransactions } from './providerDebtApi';
import type { ProviderTxn } from './providerDebtApi';

/** Lịch sử giao dịch (sổ công nợ) của 1 NCC — mở từ báo cáo công nợ NCC. */
export function ProviderTransactionsModal({
  providerId,
  providerName,
  onClose,
}: {
  providerId: string | null;
  providerName: string;
  onClose: () => void;
}) {
  const query = useProviderTransactions(providerId);
  const data = query.data;
  const rows = data?.transactions ?? [];

  const columns: ColumnsType<ProviderTxn> = [
    {
      title: 'Ngày',
      key: 'date',
      width: 110,
      render: (_: unknown, r: ProviderTxn) => (
        <CellDate value={r.date ? dayjs(r.date).format('DD/MM/YYYY') : '—'} />
      ),
    },
    {
      title: 'Loại',
      key: 'type',
      width: 110,
      render: (_: unknown, r: ProviderTxn) =>
        r.type === 'payment' ? <Tag color="green">Thanh toán</Tag> : <Tag color="default">Chi phí</Tag>,
    },
    {
      title: 'Chứng từ',
      key: 'refCode',
      width: 130,
      render: (_: unknown, r: ProviderTxn) => <CellText mono>{r.refCode ?? '—'}</CellText>,
    },
    {
      title: 'Diễn giải',
      key: 'description',
      render: (_: unknown, r: ProviderTxn) => <CellText>{r.description ?? '—'}</CellText>,
    },
    {
      title: 'Nợ',
      key: 'debit',
      width: 130,
      align: 'right',
      render: (_: unknown, r: ProviderTxn) =>
        r.debit ? <CellMoney value={r.debit} /> : <CellMoney value="—" tone="muted" />,
    },
    {
      title: 'Có',
      key: 'credit',
      width: 130,
      align: 'right',
      render: (_: unknown, r: ProviderTxn) =>
        r.credit ? <CellMoney value={r.credit} tone="success" /> : <CellMoney value="—" tone="muted" />,
    },
    {
      title: 'Còn lại',
      key: 'runningRemaining',
      width: 140,
      align: 'right',
      render: (_: unknown, r: ProviderTxn) => (
        <CellMoney value={r.runningRemaining ?? 0} tone={(r.runningRemaining ?? 0) > 0 ? 'danger' : 'muted'} />
      ),
    },
  ];

  const summary = data?.summary;

  return (
    <Modal
      open={!!providerId}
      width={900}
      title={`Lịch sử giao dịch – ${data?.providerName ?? providerName}`}
      onCancel={onClose}
      footer={null}
    >
      <div style={{ display: 'flex', gap: 24, marginBottom: 16, flexWrap: 'wrap' }}>
        <div>
          <div className="rf-stat__label">Tổng chi phí</div>
          <div style={{ marginTop: 4 }}>
            <CellMoney value={summary?.totalCost ?? 0} />
          </div>
        </div>
        <div>
          <div className="rf-stat__label">Đã trả</div>
          <div style={{ marginTop: 4 }}>
            <CellMoney value={summary?.totalPaid ?? 0} tone="success" />
          </div>
        </div>
        <div>
          <div className="rf-stat__label">Còn lại</div>
          <div style={{ marginTop: 4 }}>
            <CellMoney value={summary?.remaining ?? 0} tone={(summary?.remaining ?? 0) > 0 ? 'danger' : 'muted'} />
          </div>
        </div>
      </div>

      <Table
        rowKey={(r: ProviderTxn, i?: number) => `${r.refCode ?? ''}-${r.date}-${i ?? 0}`}
        columns={columns}
        dataSource={rows}
        loading={query.isLoading}
        pagination={false}
      />
    </Modal>
  );
}
