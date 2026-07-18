import { useState } from 'react';
import dayjs from 'dayjs';
import { App, Button, DatePicker, Drawer, Input, InputNumber, Popconfirm, Select, Space, Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { money } from '../../shared/format';
import { errorMessage } from '../../shared/api/problem';
import { CellMoney, CellDate, CellText } from '../../shared/ui/TableCells';
import { useAuth } from '../auth/AuthContext';
import {
  PAYMENT_TERM_STATUS,
  usePaymentTerms,
  useCreatePaymentTerm,
  useUpdatePaymentTerm,
  useDeletePaymentTerm,
} from './paymentTermsApi';
import type { PaymentTerm, PaymentTermForm } from './paymentTermsApi';

const STATUS_COLOR: Record<number, string> = { 0: 'gold', 1: 'green' };
const STATUS_OPTIONS = Object.entries(PAYMENT_TERM_STATUS).map(([value, label]) => ({ value: Number(value), label }));

type Draft = { amount: number; dueDate: string | null; paidAmount: number; status: number; note: string };

const emptyDraft = (): Draft => ({ amount: 0, dueDate: dayjs().toISOString(), paidAmount: 0, status: 0, note: '' });

/** Lịch thanh toán NCC của 1 booking dịch vụ — bảng đợt chi + thêm/sửa/xoá. */
export function PaymentScheduleDrawer({
  bookingId,
  bookingLabel,
  onClose,
}: {
  bookingId: string;
  bookingLabel?: string;
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('servicebooking.manage');

  const terms = usePaymentTerms(bookingId);
  const create = useCreatePaymentTerm(bookingId);
  const update = useUpdatePaymentTerm(bookingId);
  const remove = useDeletePaymentTerm(bookingId);

  const [editing, setEditing] = useState<PaymentTerm | 'new' | null>(null);
  const [draft, setDraft] = useState<Draft>(emptyDraft);

  const openNew = () => {
    setEditing('new');
    setDraft(emptyDraft());
  };
  const openEdit = (t: PaymentTerm) => {
    setEditing(t);
    setDraft({ amount: t.amount, dueDate: t.dueDate, paidAmount: t.paidAmount, status: t.status, note: t.note ?? '' });
  };

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submit() {
    if (!draft.dueDate) {
      message.error('Vui lòng chọn hạn thanh toán');
      return;
    }
    const body: PaymentTermForm = {
      amount: draft.amount,
      dueDate: draft.dueDate,
      paidAmount: draft.paidAmount,
      status: draft.status,
      note: draft.note || null,
    };
    if (editing && editing !== 'new') {
      await run(async () => {
        await update.mutateAsync({ id: editing.id, body });
        setEditing(null);
      }, 'Đã cập nhật đợt thanh toán');
    } else {
      await run(async () => {
        await create.mutateAsync(body);
        setEditing(null);
      }, 'Đã thêm đợt thanh toán');
    }
  }

  async function onDelete(id: string) {
    await run(async () => {
      await remove.mutateAsync(id);
    }, 'Đã xoá đợt thanh toán');
  }

  const rows = terms.data ?? [];
  const totalAmount = rows.reduce((s, r) => s + r.amount, 0);
  const totalPaid = rows.reduce((s, r) => s + r.paidAmount, 0);
  const totalRemaining = Math.max(0, totalAmount - totalPaid);

  const columns: ColumnsType<PaymentTerm> = [
    {
      title: 'Số tiền',
      key: 'amount',
      width: 130,
      align: 'right',
      render: (_: unknown, r: PaymentTerm) => <CellMoney value={r.amount} />,
    },
    {
      title: 'Hạn',
      key: 'dueDate',
      width: 120,
      render: (_: unknown, r: PaymentTerm) => <CellDate value={dayjs(r.dueDate).format('DD/MM/YYYY')} />,
    },
    {
      title: 'Đã trả',
      key: 'paidAmount',
      width: 130,
      align: 'right',
      render: (_: unknown, r: PaymentTerm) => <CellMoney value={r.paidAmount} tone={r.paidAmount > 0 ? 'success' : 'muted'} />,
    },
    {
      title: 'Còn lại',
      key: 'remaining',
      width: 130,
      align: 'right',
      render: (_: unknown, r: PaymentTerm) => {
        const rem = Math.max(0, r.amount - r.paidAmount);
        return <CellMoney value={rem} tone={rem > 0 ? 'danger' : 'muted'} />;
      },
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 110,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{PAYMENT_TERM_STATUS[v] ?? v}</Tag>,
    },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 110,
            render: (_: unknown, r: PaymentTerm) => (
              <Space>
                <Button size="small" onClick={() => openEdit(r)}>Sửa</Button>
                <Popconfirm title="Xoá đợt thanh toán này?" onConfirm={() => onDelete(r.id)}>
                  <Button size="small" danger>Xoá</Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<PaymentTerm>[number],
        ]
      : []),
  ];

  return (
    <Drawer
      open
      width={640}
      title={`Lịch thanh toán NCC${bookingLabel ? ` — ${bookingLabel}` : ''}`}
      onClose={onClose}
    >
      <div style={{ display: 'flex', gap: 16, marginBottom: 14, flexWrap: 'wrap' }}>
        <div>
          <div className="rf-stat__label">Tổng phải chi</div>
          <div style={{ font: '700 18px var(--tk-font-mono)', color: 'var(--tk-heading)' }}>{money(totalAmount)}</div>
        </div>
        <div>
          <div className="rf-stat__label">Đã chi</div>
          <div style={{ font: '700 18px var(--tk-font-mono)', color: 'var(--tk-success)' }}>{money(totalPaid)}</div>
        </div>
        <div>
          <div className="rf-stat__label">Còn lại</div>
          <div style={{ font: '700 18px var(--tk-font-mono)', color: 'var(--tk-danger)' }}>{money(totalRemaining)}</div>
        </div>
        {canManage && editing == null ? (
          <Button type="primary" style={{ marginLeft: 'auto', alignSelf: 'center' }} onClick={openNew}>
            Thêm đợt
          </Button>
        ) : null}
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={terms.isLoading}
        pagination={false}
      />

      {canManage && editing != null ? (
        <div className="rf-card" style={{ marginTop: 16, padding: 16 }}>
          <div className="rf-card__title" style={{ marginBottom: 12 }}>
            {editing === 'new' ? 'Thêm đợt thanh toán' : 'Sửa đợt thanh toán'}
          </div>
          <div style={{ display: 'grid', gap: 12 }}>
            <label style={{ display: 'grid', gap: 4 }}>
              <span className="rf-stat__label">Số tiền</span>
              <InputNumber
                style={{ width: '100%' }}
                min={0}
                value={draft.amount}
                onChange={(v) => setDraft((d) => ({ ...d, amount: Number(v ?? 0) }))}
              />
            </label>
            <label style={{ display: 'grid', gap: 4 }}>
              <span className="rf-stat__label">Hạn thanh toán</span>
              <DatePicker
                style={{ width: '100%' }}
                value={draft.dueDate ? dayjs(draft.dueDate) : null}
                onChange={(d) => setDraft((prev) => ({ ...prev, dueDate: d ? dayjs(d).toISOString() : null }))}
              />
            </label>
            <label style={{ display: 'grid', gap: 4 }}>
              <span className="rf-stat__label">Đã trả</span>
              <InputNumber
                style={{ width: '100%' }}
                min={0}
                value={draft.paidAmount}
                onChange={(v) => setDraft((d) => ({ ...d, paidAmount: Number(v ?? 0) }))}
              />
            </label>
            <label style={{ display: 'grid', gap: 4 }}>
              <span className="rf-stat__label">Trạng thái</span>
              <Select
                style={{ width: '100%' }}
                options={STATUS_OPTIONS}
                value={draft.status}
                onChange={(v) => setDraft((d) => ({ ...d, status: Number(v) }))}
              />
            </label>
            <label style={{ display: 'grid', gap: 4 }}>
              <span className="rf-stat__label">Ghi chú</span>
              <Input.TextArea
                rows={2}
                value={draft.note}
                onChange={(e) => setDraft((d) => ({ ...d, note: e.target.value }))}
              />
            </label>
            <Space>
              <Button type="primary" loading={create.isPending || update.isPending} onClick={submit}>
                Lưu
              </Button>
              <Button onClick={() => setEditing(null)}>Huỷ</Button>
            </Space>
          </div>
        </div>
      ) : null}

      {!canManage && rows.length === 0 && !terms.isLoading ? (
        <CellText tone="muted">Chưa có lịch thanh toán.</CellText>
      ) : null}
    </Drawer>
  );
}
