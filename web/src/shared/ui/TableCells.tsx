import type { ReactNode } from 'react';
import { money } from '../format';

/* =========================================================================
   Bộ Ô BẢNG DÙNG CHUNG (.rf-cellstack) — CHUẨN DUY NHẤT cho mọi bảng.
   Mục tiêu: gom cột thành ô giàu (dòng chính + dòng phụ) để bảng gọn,
   KHÔNG scroll ngang, và MỌI trang trông giống nhau (không mỗi chỗ 1 kiểu).
   Đừng viết inline font/màu trong column.render — hãy dùng các Ô này.
   ========================================================================= */

export type CellTone = 'default' | 'body' | 'heading' | 'muted' | 'accent' | 'success' | 'danger' | 'warning' | 'info';

const TONE: Record<CellTone, string> = {
  default: 'var(--tk-body)',
  body: 'var(--tk-body)',
  heading: 'var(--tk-heading)',
  muted: 'var(--tk-muted)',
  accent: 'var(--tk-accent)',
  success: 'var(--tk-success)',
  danger: 'var(--tk-danger)',
  warning: 'var(--tk-warning)',
  info: 'var(--tk-info)',
};

const isEmpty = (v: ReactNode): boolean => v == null || v === '' || v === '—';

/** Ô 2 dòng: dòng chính (đậm) + dòng phụ (mờ). Khối cơ bản để gom cột. */
export function CellStack({
  main,
  sub,
  tone = 'heading',
  subTone = 'muted',
  mono = false,
  subMono = false,
  align,
  title,
}: {
  main: ReactNode;
  sub?: ReactNode;
  tone?: CellTone;
  subTone?: CellTone;
  mono?: boolean;
  subMono?: boolean;
  align?: 'right' | 'center';
  title?: string;
}) {
  return (
    <div className={`rf-cellstack${align === 'right' ? ' rf-cellstack--r' : align === 'center' ? ' rf-cellstack--c' : ''}`} title={title}>
      <span
        className="rf-cellstack__main"
        style={{ color: TONE[isEmpty(main) ? 'muted' : tone], fontFamily: mono ? 'var(--tk-font-mono)' : undefined }}
      >
        {isEmpty(main) ? '—' : main}
      </span>
      {!isEmpty(sub) ? (
        <span className="rf-cellstack__sub" style={{ color: TONE[subTone], fontFamily: subMono ? 'var(--tk-font-mono)' : undefined }}>
          {sub}
        </span>
      ) : null}
    </div>
  );
}

/** Ô 1 dòng: text có tone / mono / đậm + tự thay rỗng bằng "—". */
export function CellText({
  children,
  tone = 'default',
  mono = false,
  strong = false,
  title,
}: {
  children: ReactNode;
  tone?: CellTone;
  mono?: boolean;
  strong?: boolean;
  title?: string;
}) {
  return (
    <span
      className="rf-cell1"
      title={title}
      style={{ color: TONE[isEmpty(children) ? 'muted' : tone], fontFamily: mono ? 'var(--tk-font-mono)' : undefined, fontWeight: strong ? 600 : undefined }}
    >
      {isEmpty(children) ? '—' : children}
    </span>
  );
}

/** Ô định danh: tên (đậm) + dòng phụ = mã · meta. Cho "Khách hàng", "Tour", "NCC"… */
export function CellEntity({
  name,
  code,
  meta,
  title,
}: {
  name: ReactNode;
  code?: ReactNode;
  meta?: ReactNode;
  title?: string;
}) {
  const sub =
    !isEmpty(code) && !isEmpty(meta) ? (
      <>
        {code} · {meta}
      </>
    ) : !isEmpty(code) ? (
      code
    ) : (
      meta
    );
  return <CellStack main={name} sub={sub} tone="heading" subMono={false} title={title} />;
}

/** Ô tiền: canh phải, mono, dòng chính + dòng phụ (vd thực thu / còn nợ). */
export function CellMoney({
  value,
  sub,
  tone = 'heading',
  subTone = 'muted',
  subLabel,
}: {
  value: number | ReactNode;
  sub?: number | ReactNode;
  tone?: CellTone;
  subTone?: CellTone;
  subLabel?: string;
}) {
  const subNode =
    sub == null ? undefined : typeof sub === 'number' ? (
      <>
        {subLabel ? `${subLabel} ` : ''}
        {money(sub)}
      </>
    ) : (
      sub
    );
  return <CellStack align="right" mono subMono main={typeof value === 'number' ? money(value) : value} sub={subNode} tone={tone} subTone={subTone} />;
}

/** Ô ngày: ngày chính (mono) + dòng phụ (vd ngày tạo / người tạo). */
export function CellDate({ value, sub }: { value: ReactNode; sub?: ReactNode }) {
  return <CellStack mono main={value} sub={sub} tone="heading" subTone="muted" />;
}
