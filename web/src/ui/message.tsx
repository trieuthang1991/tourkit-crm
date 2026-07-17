import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode } from 'react';
import { Icon } from './kit';

/* =========================================================================
   ui/message — hệ thống toast AntD-free, kiểu "Refined" (.rf-toast).
   Dùng: bọc app bằng <MessageProvider>, trong component: const msg = useToast();
   msg.success('...') / msg.error('...') / msg.info('...').
   ========================================================================= */

type Kind = 'success' | 'error' | 'info' | 'warning';
type Toast = { id: number; kind: Kind; text: string };

type Api = { success: (t: string) => void; error: (t: string) => void; info: (t: string) => void; warning: (t: string) => void };

const Ctx = createContext<Api | null>(null);

/** Icon + màu theo semantic token (không dùng hex rời). */
const STYLE: Record<Kind, { icon: string; color: string }> = {
  success: { icon: 'check_circle', color: 'var(--tk-success)' },
  error: { icon: 'error', color: 'var(--tk-danger)' },
  info: { icon: 'info', color: 'var(--tk-info)' },
  warning: { icon: 'warning', color: 'var(--tk-warning)' },
};

export function MessageProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const seq = useRef(0);

  const push = useCallback((kind: Kind, text: string) => {
    const id = ++seq.current;
    setToasts((ts) => [...ts, { id, kind, text }]);
    setTimeout(() => setToasts((ts) => ts.filter((t) => t.id !== id)), 3200);
  }, []);

  const api = useMemo<Api>(
    () => ({
      success: (t) => push('success', t),
      error: (t) => push('error', t),
      info: (t) => push('info', t),
      warning: (t) => push('warning', t),
    }),
    [push],
  );

  return (
    <Ctx.Provider value={api}>
      {children}
      {createPortal(
        <div className="rf-toasts">
          {toasts.map((t) => (
            <div key={t.id} className="rf-toast" role="status">
              <Icon name={STYLE[t.kind].icon} size={18} style={{ color: STYLE[t.kind].color }} />
              <span>{t.text}</span>
            </div>
          ))}
        </div>,
        document.body,
      )}
    </Ctx.Provider>
  );
}

export function useToast(): Api {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('useToast phải nằm trong <MessageProvider>');
  return ctx;
}
