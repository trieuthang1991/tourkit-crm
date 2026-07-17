import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode } from 'react';
import { Icon } from './kit';

/* =========================================================================
   ui/overlay — Modal, Drawer (trượt phải), Dropdown, Popconfirm.
   Hệ "Refined" (.rf-dialog / .rf-menu / .rf-pop). KHÔNG antd.
   ========================================================================= */

function useEsc(open: boolean, onClose: () => void) {
  useEffect(() => {
    if (!open) return;
    const h = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    document.addEventListener('keydown', h);
    return () => document.removeEventListener('keydown', h);
  }, [open, onClose]);
}

/** Khoá cuộn nền khi mở lớp phủ. */
function useLockScroll(open: boolean) {
  useEffect(() => {
    if (!open) return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = prev;
    };
  }, [open]);
}

/** Đóng khi click ra ngoài — dùng cho Dropdown/Popconfirm. */
function useClickOutside(open: boolean, ref: React.RefObject<HTMLElement | null>, close: () => void) {
  useEffect(() => {
    if (!open) return;
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) close();
    };
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, [open, ref, close]);
}

function CloseBtn({ onClick }: { onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="rf-iconbtn" aria-label="Đóng">
      <Icon name="close" size={20} />
    </button>
  );
}

/** Drawer trượt từ phải — dùng cho form CRUD. */
export function Drawer({
  open,
  title,
  onClose,
  footer,
  width = 560,
  children,
}: {
  open: boolean;
  title: ReactNode;
  onClose: () => void;
  footer?: ReactNode;
  width?: number;
  children: ReactNode;
}) {
  useEsc(open, onClose);
  useLockScroll(open);
  if (!open) return null;
  return createPortal(
    <>
      <div className="rf-mask" onClick={onClose} />
      <div className="rf-dialog rf-drawer" style={{ width: `min(${width}px, 96vw)` }} role="dialog" aria-modal="true">
        <div className="rf-dialog__head">
          <div className="rf-dialog__title">{title}</div>
          <CloseBtn onClick={onClose} />
        </div>
        <div className="rf-dialog__body" style={{ flex: 1 }}>
          {children}
        </div>
        {footer ? <div className="rf-dialog__foot">{footer}</div> : null}
      </div>
    </>,
    document.body,
  );
}

/** Modal căn giữa. */
export function Modal({
  open,
  title,
  onClose,
  footer,
  width = 520,
  children,
}: {
  open: boolean;
  title: ReactNode;
  onClose: () => void;
  footer?: ReactNode;
  width?: number;
  children: ReactNode;
}) {
  useEsc(open, onClose);
  useLockScroll(open);
  if (!open) return null;
  return createPortal(
    <div className="rf-mask" style={{ overflow: 'auto', padding: '0 16px' }} onClick={onClose}>
      <div className="rf-dialog rf-modal" style={{ maxWidth: width }} role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <div className="rf-dialog__head">
          <div className="rf-dialog__title">{title}</div>
          <CloseBtn onClick={onClose} />
        </div>
        <div className="rf-dialog__body">{children}</div>
        {footer ? <div className="rf-dialog__foot">{footer}</div> : null}
      </div>
    </div>,
    document.body,
  );
}

/** Dropdown menu popover. */
export function Dropdown({
  trigger,
  items,
}: {
  trigger: ReactNode;
  items: { key: string; label: ReactNode; icon?: string; danger?: boolean; onClick?: () => void }[];
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useClickOutside(open, ref, () => setOpen(false));
  return (
    <div ref={ref} style={{ position: 'relative', display: 'inline-block' }}>
      <span onClick={() => setOpen((o) => !o)} style={{ cursor: 'pointer' }}>
        {trigger}
      </span>
      {open ? (
        <div className="rf-menu">
          {items.map((it) => (
            <button
              key={it.key}
              type="button"
              onClick={() => {
                it.onClick?.();
                setOpen(false);
              }}
              className={`rf-menu__item ${it.danger ? 'rf-menu__item--danger' : ''}`}
            >
              {it.icon ? <Icon name={it.icon} size={18} /> : null}
              {it.label}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

/** Popconfirm — xác nhận (vd xoá). */
export function Popconfirm({
  title,
  okText = 'Xoá',
  onConfirm,
  children,
}: {
  title: string;
  okText?: string;
  onConfirm: () => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useClickOutside(open, ref, () => setOpen(false));
  return (
    <div ref={ref} style={{ position: 'relative', display: 'inline-block' }}>
      <span onClick={() => setOpen((o) => !o)}>{children}</span>
      {open ? (
        <div className="rf-pop">
          <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
            <Icon name="help" size={18} style={{ color: 'var(--tk-warning)', flexShrink: 0 }} />
            <div style={{ fontSize: 13, color: 'var(--tk-text-strong)', lineHeight: 1.5 }}>{title}</div>
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
            <button type="button" className="rf-btn rf-btn--ghost rf-btn--sm" onClick={() => setOpen(false)}>
              Huỷ
            </button>
            <button
              type="button"
              className="rf-btn rf-btn--danger rf-btn--sm"
              onClick={() => {
                onConfirm();
                setOpen(false);
              }}
            >
              {okText}
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
