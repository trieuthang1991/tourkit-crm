import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode } from 'react';

/* =========================================================================
   ui/overlay — Modal, Drawer (trượt phải), Dropdown, Popconfirm. AntD-free.
   ========================================================================= */

function useEsc(open: boolean, onClose: () => void) {
  useEffect(() => {
    if (!open) return;
    const h = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    document.addEventListener('keydown', h);
    return () => document.removeEventListener('keydown', h);
  }, [open, onClose]);
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
  if (!open) return null;
  return createPortal(
    <div className="fixed inset-0 z-[1000]">
      <div className="absolute inset-0 bg-black/35 animate-[fadeIn_.15s_ease]" onClick={onClose} />
      <div
        className="absolute right-0 top-0 flex h-full flex-col bg-white shadow-[-8px_0_32px_-8px_rgba(0,0,0,0.25)] animate-[slideIn_.2s_ease]"
        style={{ width: `min(${width}px, 96vw)` }}
      >
        <div className="flex items-center justify-between border-b border-[#f1eff5] px-5 py-4">
          <div className="text-[16px] font-semibold text-[#5e5873]">{title}</div>
          <button type="button" onClick={onClose} className="text-[#a8a5b5] hover:text-[#5e5873]">✕</button>
        </div>
        <div className="flex-1 overflow-auto p-5">{children}</div>
        {footer ? <div className="border-t border-[#f1eff5] px-5 py-3">{footer}</div> : null}
      </div>
      <style>{`@keyframes slideIn{from{transform:translateX(100%)}to{transform:translateX(0)}}@keyframes fadeIn{from{opacity:0}to{opacity:1}}`}</style>
    </div>,
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
  if (!open) return null;
  return createPortal(
    <div className="fixed inset-0 z-[1000] flex items-start justify-center overflow-auto p-6">
      <div className="absolute inset-0 bg-black/35" onClick={onClose} />
      <div className="relative z-10 mt-[8vh] w-full rounded-[14px] bg-white shadow-[0_24px_60px_-12px_rgba(0,0,0,0.35)]" style={{ maxWidth: width }}>
        <div className="flex items-center justify-between border-b border-[#f1eff5] px-5 py-4">
          <div className="text-[16px] font-semibold text-[#5e5873]">{title}</div>
          <button type="button" onClick={onClose} className="text-[#a8a5b5] hover:text-[#5e5873]">✕</button>
        </div>
        <div className="p-5">{children}</div>
        {footer ? <div className="border-t border-[#f1eff5] px-5 py-3">{footer}</div> : null}
      </div>
    </div>,
    document.body,
  );
}

/** Dropdown menu popover. */
export function Dropdown({ trigger, items }: { trigger: ReactNode; items: { key: string; label: ReactNode; danger?: boolean; onClick?: () => void }[] }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    const h = (e: MouseEvent) => ref.current && !ref.current.contains(e.target as Node) && setOpen(false);
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, [open]);
  return (
    <div ref={ref} className="relative inline-block">
      <span onClick={() => setOpen((o) => !o)} className="cursor-pointer">{trigger}</span>
      {open ? (
        <div className="absolute right-0 z-50 mt-1 min-w-[160px] overflow-hidden rounded-[10px] border border-[#eee] bg-white py-1 shadow-[0_10px_30px_-8px_rgba(34,41,47,0.22)]">
          {items.map((it) => (
            <button
              key={it.key}
              type="button"
              onClick={() => {
                it.onClick?.();
                setOpen(false);
              }}
              className={`block w-full px-4 py-2 text-left text-[13px] hover:bg-[#faf7f5] ${it.danger ? 'text-[#d1494a]' : 'text-[#5e5873]'}`}
            >
              {it.label}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

/** Popconfirm — xác nhận (vd xoá). */
export function Popconfirm({ title, onConfirm, children }: { title: string; onConfirm: () => void; children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    const h = (e: MouseEvent) => ref.current && !ref.current.contains(e.target as Node) && setOpen(false);
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, [open]);
  return (
    <div ref={ref} className="relative inline-block">
      <span onClick={() => setOpen((o) => !o)}>{children}</span>
      {open ? (
        <div className="absolute right-0 z-50 mt-1 w-[220px] rounded-[10px] border border-[#eee] bg-white p-3 shadow-[0_10px_30px_-8px_rgba(34,41,47,0.22)]">
          <div className="mb-2 text-[13px] text-[#5e5873]">{title}</div>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setOpen(false)} className="rounded-md border border-[#e6e3ee] px-2.5 py-1 text-[12px] text-[#6e6b7b]">Huỷ</button>
            <button
              type="button"
              onClick={() => {
                onConfirm();
                setOpen(false);
              }}
              className="rounded-md bg-[#d1494a] px-2.5 py-1 text-[12px] text-white"
            >
              Xoá
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
