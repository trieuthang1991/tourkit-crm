import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { Modal } from '../../ui/overlay';
import { Button, StatusTag } from '../../ui/kit';
import { voucherTone } from '../../shared/ui';
import { money } from '../../shared/format';
import { VOUCHER_STATUS } from './listTypes';
import type { PaymentListItem } from './listTypes';

/* Modal chi tiết phiếu chi — mở khi kích 1 dòng ở danh sách (không nhảy trang). */

const dateVi = (v: string) => new Date(v).toLocaleDateString('vi-VN');

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 16, padding: '9px 0', borderBottom: '1px solid var(--tk-line)' }}>
      <span style={{ color: 'var(--tk-muted)', fontSize: 13 }}>{label}</span>
      <span style={{ color: 'var(--tk-heading)', fontWeight: 500, fontSize: 13, textAlign: 'right' }}>{children}</span>
    </div>
  );
}

export function PaymentDetailModal({
  payment,
  onClose,
  canApprove,
  onAct,
}: {
  payment: PaymentListItem | null;
  onClose: () => void;
  canApprove: boolean;
  onAct: (id: string, action: 'approve' | 'reject') => void;
}) {
  const navigate = useNavigate();
  const p = payment;
  const isPending = p?.status === 0;

  return (
    <Modal
      open={!!p}
      onClose={onClose}
      width={520}
      title={p ? `Phiếu chi ${p.code}` : ''}
      footer={
        p ? (
          <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', gap: 8 }}>
            <Button
              variant="ghost"
              icon="open_in_new"
              onClick={() => {
                onClose();
                navigate(`/orders/${p.orderId}`);
              }}
            >
              Mở đơn liên quan
            </Button>
            {canApprove && isPending ? (
              <span style={{ display: 'inline-flex', gap: 8 }}>
                <Button variant="danger" icon="close" onClick={() => { onAct(p.id, 'reject'); onClose(); }}>
                  Từ chối
                </Button>
                <Button variant="primary" icon="check" onClick={() => { onAct(p.id, 'approve'); onClose(); }}>
                  Duyệt
                </Button>
              </span>
            ) : null}
          </div>
        ) : null
      }
    >
      {p ? (
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 14 }}>
            <span style={{ font: '700 22px var(--tk-font-mono)', color: 'var(--tk-danger)' }}>{money(p.amount)}</span>
            <StatusTag tone={voucherTone(p.status)}>{VOUCHER_STATUS[p.status] ?? p.status}</StatusTag>
          </div>
          <Field label="Ngày lập">{dateVi(p.issuedAt)}</Field>
          <Field label="Nhà cung cấp">{p.providerName ?? '—'}</Field>
          <Field label="Người nhận">{p.receiverName ?? '—'}</Field>
          <Field label="Mã đơn">{p.orderCode ?? '—'}</Field>
          <Field label="Hình thức">{p.paymentMethod}</Field>
          <Field label="Ghi nhận dòng tiền">{p.isRecognized ? 'Đã ghi nhận' : 'Chưa ghi nhận'}</Field>
        </div>
      ) : null}
    </Modal>
  );
}
