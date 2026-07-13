import { Modal as AntModal } from 'antd';
import type { ReactNode } from 'react';
import { Button } from './Button';

// Modal chuẩn: tiêu đề + mô tả, footer Huỷ / Lưu. Bọc AntD Modal.
export function Modal({
  open,
  title,
  desc,
  onCancel,
  onSubmit,
  submitting,
  submitLabel = 'Lưu',
  width = 640,
  children,
}: {
  open: boolean;
  title: ReactNode;
  desc?: string;
  onCancel: () => void;
  onSubmit?: () => void;
  submitting?: boolean;
  submitLabel?: string;
  width?: number;
  children: ReactNode;
}) {
  return (
    <AntModal
      open={open}
      onCancel={onCancel}
      width={width}
      title={
        <div>
          <div style={{ fontSize: 17, fontWeight: 700, color: 'var(--tk-heading)' }}>{title}</div>
          {desc ? <div style={{ fontSize: 12.5, color: 'var(--tk-muted)', marginTop: 2, fontWeight: 400 }}>{desc}</div> : null}
        </div>
      }
      footer={
        onSubmit
          ? [
              <Button key="cancel" variant="ghost" onClick={onCancel}>Huỷ</Button>,
              <Button key="ok" variant="primary" loading={submitting} onClick={onSubmit}>{submitLabel}</Button>,
            ]
          : null
      }
    >
      {children}
    </AntModal>
  );
}
