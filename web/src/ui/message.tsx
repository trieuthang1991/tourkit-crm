import { createContext, useCallback, useContext, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode } from 'react';

/* =========================================================================
   ui/message — hệ thống toast AntD-free. Thay App.useApp().message.
   Dùng: bọc app bằng <MessageProvider>, trong component: const msg = useToast();
   msg.success('...') / msg.error('...') / msg.info('...').
   ========================================================================= */

type Kind = 'success' | 'error' | 'info' | 'warning';
type Toast = { id: number; kind: Kind; text: string };

type Api = { success: (t: string) => void; error: (t: string) => void; info: (t: string) => void; warning: (t: string) => void };

const Ctx = createContext<Api | null>(null);

const STYLE: Record<Kind, { bar: string; icon: string }> = {
  success: { bar: '#28c76f', icon: '✓' },
  error: { bar: '#ea5455', icon: '✕' },
  info: { bar: '#4e7bff', icon: 'i' },
  warning: { bar: '#ff9f43', icon: '!' },
};

export function MessageProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const seq = useRef(0);

  const push = useCallback((kind: Kind, text: string) => {
    const id = ++seq.current;
    setToasts((ts) => [...ts, { id, kind, text }]);
    setTimeout(() => setToasts((ts) => ts.filter((t) => t.id !== id)), 3200);
  }, []);

  const api: Api = {
    success: (t) => push('success', t),
    error: (t) => push('error', t),
    info: (t) => push('info', t),
    warning: (t) => push('warning', t),
  };

  return (
    <Ctx.Provider value={api}>
      {children}
      {createPortal(
        <div className="pointer-events-none fixed left-1/2 top-4 z-[2000] flex -translate-x-1/2 flex-col items-center gap-2">
          {toasts.map((t) => (
            <div
              key={t.id}
              className="pointer-events-auto flex items-center gap-3 rounded-[10px] border border-[#eef0f5] bg-white px-4 py-2.5 shadow-[0_10px_30px_-8px_rgba(34,41,47,0.25)]"
              style={{ borderLeft: `4px solid ${STYLE[t.kind].bar}` }}
            >
              <span
                className="flex h-5 w-5 items-center justify-center rounded-full text-[12px] font-bold text-white"
                style={{ background: STYLE[t.kind].bar }}
              >
                {STYLE[t.kind].icon}
              </span>
              <span className="text-[13px] text-[#5e5873]">{t.text}</span>
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
