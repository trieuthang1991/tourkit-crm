import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { Modal } from '../../ui/overlay';
import { Button, StatusTag } from '../../ui/kit';
import { voucherTone } from '../../shared/ui';
import { money } from '../../shared/format';
import { VOUCHER_STATUS } from './listTypes';
import type { ReceiptListItem } from './listTypes';

/* Modal chi tiết phiếu thu — mở khi kích 1 dòng ở danh sách (không nhảy trang).
   Hiển thị đủ thông tin phiếu + duyệt/từ chối tại chỗ + mở đơn liên quan. */

const dateVi = (v: string) => new Date(v).toLocaleDateString('vi-VN');

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 16, padding: '9px 0', borderBottom: '1px solid var(--tk-line)' }}>
      <span style={{ color: 'var(--tk-muted)', fontSize: 13 }}>{label}</span>
      <span style={{ color: 'var(--tk-heading)', fontWeight: 500, fontSize: 13, textAlign: 'right' }}>{children}</span>
    </div>
  );
}

export function ReceiptDetailModal({
  receipt,
  onClose,
  canApprove,
  onAct,
}: {
  receipt: ReceiptListItem | null;
  onClose: () => void;
  canApprove: boolean;
  onAct: (id: string, action: 'approve' | 'reject') => void;
}) {
  const navigate = useNavigate();
  const r = receipt;
  const isPending = r?.status === 0;

  return (
    <Modal
      open={!!r}
      onClose={onClose}
      width={520}
      title={r ? `Phiếu thu ${r.code}` : ''}
      footer={
        r ? (
          <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', gap: 8 }}>
            <Button
              variant="ghost"
              icon="open_in_new"
              onClick={() => {
                onClose();
                navigate(`/orders/${r.orderId}`);
              }}
            >
              Mở đơn liên quan
            </Button>
            {canApprove && isPending ? (
              <span style={{ display: 'inline-flex', gap: 8 }}>
                <Button variant="danger" icon="close" onClick={() => { onAct(r.id, 'reject'); onClose(); }}>
                  Từ chối
                </Button>
                <Button variant="primary" icon="check" onClick={() => { onAct(r.id, 'approve'); onClose(); }}>
                  Duyệt
                </Button>
              </span>
            ) : null}
          </div>
        ) : null
      }
    >
      {r ? (
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 14 }}>
            <span style={{ font: '700 22px var(--tk-font-mono)', color: 'var(--tk-success)' }}>{money(r.amount)}</span>
            <StatusTag tone={voucherTone(r.status)}>{VOUCHER_STATUS[r.status] ?? r.status}</StatusTag>
          </div>
          <Field label="Ngày lập">{dateVi(r.issuedAt)}</Field>
          <Field label="Khách hàng">{r.customerName ?? '—'}</Field>
          <Field label="Mã đơn">{r.orderCode ?? '—'}</Field>
          <Field label="Người nộp">{r.partner ?? '—'}</Field>
          <Field label="Hình thức">{r.paymentMethod}</Field>
          <Field label="Ghi nhận dòng tiền">{r.isRecognized ? 'Đã ghi nhận' : 'Chưa ghi nhận'}</Field>
        </div>
      ) : null}
    </Modal>
  );
}
