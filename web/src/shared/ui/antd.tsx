/* =========================================================================
   shared/ui/antd — SHIM tương thích API AntD, render bằng hệ "Refined" (ui/*).
   Mục tiêu: 86 màn import '{ ... } from shared/ui/antd' KHÔNG phải sửa,
   nhưng runtime KHÔNG còn dùng antd cho các component phổ biến.

   Thủ thuật type: mỗi shim được CAST sang đúng type component antd
   (`as unknown as A['X']`) — pages typecheck y hệt trước (kể cả generic +
   static như Table.Summary), còn runtime là markup .rf-* / ui/*.
   Giữ real-antd runtime cho vài component hiếm (Form/List/Calendar/Steps/Result).
   ========================================================================= */
import type { CSSProperties, ReactNode } from 'react';
import { Fragment, forwardRef, isValidElement } from 'react';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import type * as AntdNS from 'antd';
import type { ColumnsType, ColumnType } from 'antd/es/table';

import { Icon, Empty as RfEmpty } from '../../ui/kit';
import { Select as RfSelect } from '../../ui/inputs';
import { Modal as RfModal, Drawer as RfDrawer, Popconfirm as RfPopconfirm } from '../../ui/overlay';
import {
  Tag as RfTag,
  Text as RfText,
  Title as RfTitle,
  Link as RfLink,
  Alert as RfAlert,
  Divider as RfDivider,
  Progress as RfProgress,
  Checkbox as RfCheckbox,
} from '../../ui/primitives';
import { Tabs as RfTabs } from '../../ui/kit';
import { useToastOptional } from '../../ui/message';

type A = typeof AntdNS;

export type { ColumnsType } from 'antd/es/table';

// KHÔNG còn re-export runtime nào từ 'antd' — toàn bộ component đã shim bằng ui/*.
// (antd chỉ còn được dùng ở dạng `import type` cho type-fidelity của các shim.)

const cast = <K extends keyof A>(impl: unknown): A[K] => impl as A[K];
const asProps = (p: unknown) => p as Record<string, unknown>;

/* ---------------------- App / message ---------------------- */
function AppImpl({ children }: { children?: ReactNode }) {
  return <>{children}</>;
}
AppImpl.useApp = function useApp() {
  const t = useToastOptional();
  const asStr = (fn: (s: string) => void) => (m: ReactNode) => fn(typeof m === 'string' ? m : String(m ?? ''));
  const api = { success: asStr(t.success), error: asStr(t.error), info: asStr(t.info), warning: asStr(t.warning), open: () => {}, loading: () => {}, warn: asStr(t.warning), destroy: () => {} };
  return { message: api, notification: api, modal: {} };
};
export const App = cast<'App'>(AppImpl);

/* ---------------------- Button ---------------------- */
function ButtonImpl(props: Record<string, unknown>) {
  const { type, variant, danger, size, loading, icon, leftIcon, disabled, htmlType, href, onClick, children, style, className, block } = asProps(props);
  const v =
    (variant as string) ??
    (danger ? 'danger' : type === 'primary' ? 'primary' : type === 'text' || type === 'link' ? 'text' : 'ghost');
  const cls = `rf-btn rf-btn--${v} ${size === 'small' ? 'rf-btn--sm' : ''} ${type === 'link' ? 'rf-btn--link' : ''} ${(className as string) ?? ''}`;
  const content = (
    <>
      {loading ? <span className="rf-spin" /> : ((icon as ReactNode) ?? (leftIcon as ReactNode))}
      {children as ReactNode}
    </>
  );
  const st: CSSProperties = { ...(block ? { width: '100%' } : null), ...(style as CSSProperties) };
  if (href) {
    return (
      <a className={cls} href={href as string} style={st} onClick={onClick as React.MouseEventHandler<HTMLAnchorElement>}>
        {content}
      </a>
    );
  }
  return (
    <button type={htmlType === 'submit' ? 'submit' : 'button'} className={cls} style={st} disabled={!!(disabled || loading)} onClick={onClick as React.MouseEventHandler<HTMLButtonElement>}>
      {content}
    </button>
  );
}
export const Button = cast<'Button'>(ButtonImpl);

/* ---------------------- Input (+ Search/TextArea/Password/Group) ---------------------- */
const BaseInput = forwardRef<HTMLInputElement, Record<string, unknown>>(function BaseInput(props, ref) {
  const { value, onChange, placeholder, prefix, suffix, allowClear, onPressEnter, style, disabled, type, maxLength, className } = asProps(props);
  const has = value != null && value !== '';
  return (
    <div className={`rf-field ${(className as string) ?? ''}`} style={style as CSSProperties}>
      {prefix as ReactNode}
      <input
        ref={ref}
        value={(value as string | number | undefined) ?? ''}
        placeholder={placeholder as string}
        disabled={disabled as boolean}
        type={type as string}
        maxLength={maxLength as number}
        onChange={onChange as React.ChangeEventHandler<HTMLInputElement>}
        onKeyDown={(e) => e.key === 'Enter' && (onPressEnter as ((e: unknown) => void) | undefined)?.(e)}
      />
      {allowClear && has ? (
        <button
          type="button"
          aria-label="Xoá"
          onClick={() => (onChange as ((e: unknown) => void) | undefined)?.({ target: { value: '' } })}
          style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: 'var(--tk-muted)', lineHeight: 1 }}
        >
          ✕
        </button>
      ) : null}
      {suffix as ReactNode}
    </div>
  );
});
function SearchInputImpl(props: Record<string, unknown>) {
  const { onSearch, enterButton, style, ...rest } = asProps(props);
  const os = onSearch as ((v: string) => void) | undefined;
  return (
    <div style={{ display: 'flex', gap: 8, ...(style as CSSProperties) }}>
      <BaseInput {...rest} style={{ flex: 1 }} prefix={<Icon name="search" size={18} style={{ color: 'var(--tk-muted)' }} />} onPressEnter={(e: { target: HTMLInputElement }) => os?.(e.target.value)} />
      {enterButton ? (
        <ButtonImpl type="primary" onClick={() => os?.(String((rest as { value?: unknown }).value ?? ''))}>
          {enterButton === true ? <Icon name="search" size={18} /> : (enterButton as ReactNode)}
        </ButtonImpl>
      ) : null}
    </div>
  );
}
const TextAreaImpl = forwardRef<HTMLTextAreaElement, Record<string, unknown>>(function TextAreaImpl(props, ref) {
  const { value, onChange, placeholder, rows = 4, style, disabled, maxLength } = asProps(props);
  return (
    <textarea
      ref={ref}
      value={(value as string | undefined) ?? ''}
      placeholder={placeholder as string}
      rows={rows as number}
      disabled={disabled as boolean}
      maxLength={maxLength as number}
      onChange={onChange as React.ChangeEventHandler<HTMLTextAreaElement>}
      style={{
        width: '100%', padding: '9px 11px', borderRadius: 'var(--tk-radius)', border: '1px solid var(--tk-border)',
        background: 'var(--tk-surface)', font: '400 13px var(--tk-font)', color: 'var(--tk-heading)', outline: 'none', resize: 'vertical', lineHeight: 1.6,
        ...(style as CSSProperties),
      }}
    />
  );
});
const PasswordImpl = forwardRef<HTMLInputElement, Record<string, unknown>>(function PasswordImpl(props, ref) {
  return <BaseInput {...props} ref={ref} type="password" />;
});
const InputWithStatics = Object.assign(BaseInput, {
  Search: SearchInputImpl,
  TextArea: TextAreaImpl,
  Password: PasswordImpl,
  Group: ({ children, style }: { children?: ReactNode; style?: CSSProperties }) => <div style={{ display: 'flex', gap: 6, ...style }}>{children}</div>,
});
export const Input = cast<'Input'>(InputWithStatics);

/* ---------------------- InputNumber ---------------------- */
function InputNumberImpl(props: Record<string, unknown>) {
  const { value, onChange, min, max, step, placeholder, style, disabled } = asProps(props);
  const oc = onChange as ((v: number | null) => void) | undefined;
  return (
    <div className="rf-field" style={style as CSSProperties}>
      <input
        type="number"
        inputMode="decimal"
        style={{ fontFamily: 'var(--tk-font-mono)' }}
        value={(value as number | string | undefined) ?? ''}
        min={min as number}
        max={max as number}
        step={step as number}
        disabled={disabled as boolean}
        placeholder={(placeholder as string) ?? '0'}
        onChange={(e) => oc?.(e.target.value === '' ? null : Number(e.target.value))}
      />
    </div>
  );
}
export const InputNumber = cast<'InputNumber'>(InputNumberImpl);

/* ---------------------- Select ---------------------- */
function SelectImpl(props: Record<string, unknown>) {
  const { value, onChange, options, mode, showSearch, allowClear, placeholder, style, disabled } = asProps(props);
  const multiple = mode === 'multiple' || mode === 'tags';
  return (
    <RfSelect
      value={value as never}
      onChange={(v) => (onChange as ((v: unknown, o?: unknown) => void) | undefined)?.(v, undefined)}
      options={(options ?? []) as { label: string; value: string | number }[]}
      placeholder={placeholder as string}
      allowClear={allowClear as boolean}
      showSearch={showSearch as boolean}
      multiple={multiple}
      tags={mode === 'tags'}
      style={{ ...(disabled ? { opacity: 0.6, pointerEvents: 'none' } : null), ...(style as CSSProperties) }}
    />
  );
}
export const Select = cast<'Select'>(SelectImpl);

/* ---------------------- Space ---------------------- */
function SpaceImpl(props: Record<string, unknown>) {
  const { children, size = 'small', direction = 'horizontal', wrap, align, style } = asProps(props);
  const one = (x: unknown): number => (x === 'small' ? 8 : x === 'middle' ? 12 : x === 'large' ? 16 : typeof x === 'number' ? x : 8);
  const gap = Array.isArray(size) ? `${one(size[1])}px ${one(size[0])}px` : `${one(size)}px`;
  return (
    <div
      style={{
        display: 'inline-flex',
        flexDirection: direction === 'vertical' ? 'column' : 'row',
        gap,
        flexWrap: wrap ? 'wrap' : undefined,
        alignItems: (align as string) ?? (direction === 'horizontal' ? 'center' : undefined),
        ...(style as CSSProperties),
      }}
    >
      {children as ReactNode}
    </div>
  );
}
export const Space = cast<'Space'>(SpaceImpl);

/* ---------------------- Row / Col ---------------------- */
function RowImpl(props: Record<string, unknown>) {
  const { children, gutter, style, align, justify } = asProps(props);
  const g = Array.isArray(gutter) ? gutter : [gutter ?? 0, gutter ?? 0];
  const gx = typeof g[0] === 'number' ? g[0] : 0;
  const gy = typeof g[1] === 'number' ? g[1] : 0;
  const jmap: Record<string, string> = { start: 'flex-start', end: 'flex-end', center: 'center', 'space-between': 'space-between', 'space-around': 'space-around' };
  return (
    <div
      style={{
        display: 'flex', flexWrap: 'wrap', rowGap: gy, columnGap: gx,
        alignItems: align === 'middle' ? 'center' : align === 'bottom' ? 'flex-end' : undefined,
        justifyContent: justify ? jmap[justify as string] : undefined,
        ...(style as CSSProperties),
      }}
    >
      {children as ReactNode}
    </div>
  );
}
function ColImpl(props: Record<string, unknown>) {
  const { children, span, flex, style } = asProps(props);
  const num = (x: unknown): number | undefined => (typeof x === 'number' ? x : undefined);
  const eff = num(props.lg) ?? num(props.md) ?? num(props.sm) ?? num(props.xs) ?? num(span) ?? 24;
  const basis = flex != null ? (typeof flex === 'number' ? `${flex} 1 0` : String(flex)) : undefined;
  return (
    <div
      style={
        flex != null
          ? { flex: basis, minWidth: 0, ...(style as CSSProperties) }
          : { flex: `0 0 ${(eff / 24) * 100}%`, maxWidth: `${(eff / 24) * 100}%`, minWidth: 0, boxSizing: 'border-box', ...(style as CSSProperties) }
      }
    >
      {children as ReactNode}
    </div>
  );
}
export const Row = cast<'Row'>(RowImpl);
export const Col = cast<'Col'>(ColImpl);

/* ---------------------- Card ---------------------- */
function CardImpl(props: Record<string, unknown>) {
  const { title, extra, children, size, style, styles, className, onClick, hoverable } = asProps(props);
  const body = (styles as { body?: CSSProperties } | undefined)?.body;
  const bodyPad = body?.padding ?? (size === 'small' ? 14 : 18);
  return (
    <div
      className={`rf-card ${hoverable ? 'rf-card--hover' : ''} ${(className as string) ?? ''}`}
      style={{ ...(onClick ? { cursor: 'pointer' } : null), ...(style as CSSProperties) }}
      onClick={onClick as React.MouseEventHandler<HTMLDivElement> | undefined}
    >
      {title || extra ? (
        <div className="rf-card__head">
          <div className="rf-card__title">{title as ReactNode}</div>
          {extra as ReactNode}
        </div>
      ) : null}
      <div style={{ padding: bodyPad, ...body }}>{children as ReactNode}</div>
    </div>
  );
}
export const Card = cast<'Card'>(CardImpl);

/* ---------------------- Typography ---------------------- */
const TypographyImpl = {
  Text: ({ children, type, strong, style }: Record<string, unknown>) => (
    <RfText type={type as 'secondary' | 'success' | 'warning' | 'danger'} strong={strong as boolean} style={style as CSSProperties}>
      {children as ReactNode}
    </RfText>
  ),
  Title: ({ children, level, style }: Record<string, unknown>) => (
    <RfTitle level={(level as 1 | 2 | 3 | 4 | 5) ?? 3} style={style as CSSProperties}>
      {children as ReactNode}
    </RfTitle>
  ),
  Paragraph: ({ children, style }: Record<string, unknown>) => (
    <p style={{ margin: '0 0 8px', fontSize: 13, color: 'var(--tk-body)', lineHeight: 1.6, ...(style as CSSProperties) }}>{children as ReactNode}</p>
  ),
  Link: ({ children, onClick, href, style }: Record<string, unknown>) => (
    <RfLink onClick={onClick as () => void} href={href as string} style={style as CSSProperties}>
      {children as ReactNode}
    </RfLink>
  ),
};
export const Typography = cast<'Typography'>(TypographyImpl);

/* ---------------------- Tag ---------------------- */
function TagImpl(props: Record<string, unknown>) {
  const { color, children, style, title } = asProps(props);
  return (
    <RfTag color={color as string} style={style as CSSProperties} title={title as string}>
      {children as ReactNode}
    </RfTag>
  );
}
export const Tag = cast<'Tag'>(TagImpl);

/* ---------------------- Statistic ---------------------- */
function StatisticImpl(props: Record<string, unknown>) {
  const { title, value, precision, suffix, prefix, valueStyle, formatter } = asProps(props);
  const shown = formatter ? (formatter as (v: unknown) => ReactNode)(value) : undefined;
  return (
    <div>
      {title != null ? <div className="rf-stat__label">{title as ReactNode}</div> : null}
      <div style={{ font: '700 22px var(--tk-font-mono)', color: 'var(--tk-heading)', letterSpacing: '-0.02em', marginTop: 6, ...(valueStyle as CSSProperties) }}>
        {prefix as ReactNode}{' '}
        {shown ?? (typeof value === 'number' ? value.toLocaleString('vi-VN', { minimumFractionDigits: (precision as number) ?? 0, maximumFractionDigits: (precision as number) ?? 0 }) : (value as ReactNode))}{' '}
        {suffix ? <span style={{ fontSize: 13, fontWeight: 500, color: 'var(--tk-muted-2)' }}>{suffix as ReactNode}</span> : null}
      </div>
    </div>
  );
}
export const Statistic = cast<'Statistic'>(StatisticImpl);

/* ---------------------- Segmented ---------------------- */
function SegmentedImpl(props: Record<string, unknown>) {
  const { options, value, onChange, block, style } = asProps(props);
  const opts = ((options ?? []) as unknown[]).map((o) => (typeof o === 'object' ? (o as { label: ReactNode; value: string | number }) : { label: o as ReactNode, value: o as string | number }));
  return (
    <div className="rf-segs" style={{ ...(block ? { display: 'flex' } : null), ...(style as CSSProperties) }}>
      {opts.map((o) => (
        <button key={String(o.value)} type="button" className={`rf-seg ${String(value) === String(o.value) ? 'rf-seg--active' : ''}`} style={block ? { flex: 1 } : undefined} onClick={() => (onChange as ((v: unknown) => void) | undefined)?.(o.value)}>
          {o.label}
        </button>
      ))}
    </div>
  );
}
export const Segmented = cast<'Segmented'>(SegmentedImpl);

/* ---------------------- DatePicker (+ RangePicker) ---------------------- */
function DatePickerImpl(props: Record<string, unknown>) {
  const { value, onChange, style } = asProps(props);
  const oc = onChange as ((d: Dayjs | null, s: string) => void) | undefined;
  const asDate = value ? dayjs(value as Dayjs).format('YYYY-MM-DD') : '';
  return (
    <div className="rf-field" style={style as CSSProperties}>
      <input type="date" style={{ fontFamily: 'var(--tk-font-mono)' }} value={asDate} onChange={(e) => oc?.(e.target.value ? dayjs(e.target.value) : null, e.target.value)} />
    </div>
  );
}
function RangePickerImpl(props: Record<string, unknown>) {
  const { value, onChange, placeholder, style } = asProps(props);
  const oc = onChange as ((d: [Dayjs | null, Dayjs | null] | null, s: [string, string]) => void) | undefined;
  const val = value as [Dayjs | null, Dayjs | null] | null | undefined;
  const ph = placeholder as [string, string] | undefined;
  const fmt = (d?: Dayjs | null) => (d ? dayjs(d).format('YYYY-MM-DD') : '');
  const from = val?.[0] ?? null;
  const to = val?.[1] ?? null;
  const emit = (f: Dayjs | null, t: Dayjs | null) => oc?.(f || t ? [f, t] : null, [f ? f.format('YYYY-MM-DD') : '', t ? t.format('YYYY-MM-DD') : '']);
  return (
    <div className="rf-field" style={{ gap: 4, ...(style as CSSProperties) }}>
      <input type="date" title={ph?.[0]} style={{ fontFamily: 'var(--tk-font-mono)' }} value={fmt(from)} onChange={(e) => emit(e.target.value ? dayjs(e.target.value) : null, to)} />
      <span style={{ color: 'var(--tk-muted)', flexShrink: 0 }}>–</span>
      <input type="date" title={ph?.[1]} style={{ fontFamily: 'var(--tk-font-mono)' }} value={fmt(to)} onChange={(e) => emit(from, e.target.value ? dayjs(e.target.value) : null)} />
    </div>
  );
}
DatePickerImpl.RangePicker = RangePickerImpl;
export const DatePicker = cast<'DatePicker'>(DatePickerImpl);

/* ---------------------- Modal ---------------------- */
function ModalImpl(props: Record<string, unknown>) {
  const { open, title, onCancel, footer, width, children, okText, cancelText, onOk, confirmLoading } = asProps(props);
  const oc = onCancel as (() => void) | undefined;
  const foot =
    footer === null ? undefined
      : footer !== undefined ? (footer as ReactNode)
        : (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
            <ButtonImpl onClick={oc}>{(cancelText as ReactNode) ?? 'Huỷ'}</ButtonImpl>
            <ButtonImpl type="primary" loading={confirmLoading} onClick={onOk as () => void}>
              {(okText as ReactNode) ?? 'OK'}
            </ButtonImpl>
          </div>
        );
  return (
    <RfModal open={!!open} title={title as ReactNode} width={typeof width === 'number' ? width : 520} onClose={() => oc?.()} footer={foot}>
      {children as ReactNode}
    </RfModal>
  );
}
export const Modal = cast<'Modal'>(ModalImpl);

/* ---------------------- Drawer ---------------------- */
function DrawerImpl(props: Record<string, unknown>) {
  const { open, title, onClose, footer, width, children } = asProps(props);
  const oc = onClose as (() => void) | undefined;
  return (
    <RfDrawer open={!!open} title={title as ReactNode} width={typeof width === 'number' ? width : 560} onClose={() => oc?.()} footer={footer as ReactNode}>
      {children as ReactNode}
    </RfDrawer>
  );
}
export const Drawer = cast<'Drawer'>(DrawerImpl);

/* ---------------------- Popconfirm ---------------------- */
function PopconfirmImpl(props: Record<string, unknown>) {
  const { title, onConfirm, okText, children } = asProps(props);
  return (
    <RfPopconfirm title={String(title ?? '')} okText={(okText as string) ?? 'Xoá'} onConfirm={() => (onConfirm as (() => void) | undefined)?.()}>
      {children as ReactNode}
    </RfPopconfirm>
  );
}
export const Popconfirm = cast<'Popconfirm'>(PopconfirmImpl);

/* ---------------------- Tabs ---------------------- */
function TabsImpl(props: Record<string, unknown>) {
  const items = ((props.items ?? []) as { key: React.Key; label: ReactNode; children?: ReactNode }[]).map((it) => ({ key: String(it.key), label: it.label, children: it.children }));
  return <RfTabs items={items} />;
}
export const Tabs = cast<'Tabs'>(TabsImpl);

/* ---------------------- Alert / Divider / Progress / Checkbox / Empty ---------------------- */
function AlertImpl(props: Record<string, unknown>) {
  const { type, message, description, style } = asProps(props);
  return <RfAlert type={(type as 'success' | 'info' | 'warning' | 'error') ?? 'info'} message={message as ReactNode} description={description as ReactNode} style={style as CSSProperties} />;
}
export const Alert = cast<'Alert'>(AlertImpl);

function DividerImpl(props: Record<string, unknown>) {
  return <RfDivider vertical={props.type === 'vertical'} style={props.style as CSSProperties} />;
}
export const Divider = cast<'Divider'>(DividerImpl);

function ProgressImpl(props: Record<string, unknown>) {
  const { percent, showInfo, strokeColor } = asProps(props);
  return <RfProgress percent={(percent as number) ?? 0} showInfo={showInfo !== false} tone={typeof strokeColor === 'string' ? strokeColor : undefined} />;
}
export const Progress = cast<'Progress'>(ProgressImpl);

function CheckboxImpl(props: Record<string, unknown>) {
  const { checked, onChange, children } = asProps(props);
  return (
    <RfCheckbox checked={!!checked} onChange={(v) => (onChange as ((e: unknown) => void) | undefined)?.({ target: { checked: v } })}>
      {children as ReactNode}
    </RfCheckbox>
  );
}
export const Checkbox = cast<'Checkbox'>(CheckboxImpl);

function EmptyImpl(props: Record<string, unknown>) {
  const d = props.description;
  return <RfEmpty text={typeof d === 'string' ? d : 'Không có dữ liệu'} />;
}
export const Empty = cast<'Empty'>(EmptyImpl);

/* ---------------------- Spin ---------------------- */
function SpinImpl(props: Record<string, unknown>) {
  const { spinning = true, children, tip, size } = asProps(props);
  const sz = size === 'large' ? 28 : size === 'small' ? 14 : 20;
  const loader = (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, padding: children ? 0 : 24 }}>
      <span className="rf-spin" style={{ width: sz, height: sz, color: 'var(--tk-accent)' }} />
      {tip ? <span style={{ fontSize: 12.5, color: 'var(--tk-muted)' }}>{tip as ReactNode}</span> : null}
    </div>
  );
  if (children == null) return spinning ? loader : null;
  return (
    <div style={{ position: 'relative' }}>
      {children as ReactNode}
      {spinning ? (
        <div style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(255,255,255,0.6)' }}>{loader}</div>
      ) : null}
    </div>
  );
}
export const Spin = cast<'Spin'>(SpinImpl);

/* ---------------------- Descriptions (+ Item) ---------------------- */
function DescriptionsImpl(props: Record<string, unknown>) {
  const { title, children, column } = asProps(props);
  const cols = typeof column === 'number' ? column : 1;
  return (
    <div>
      {title ? <div className="rf-card__title" style={{ marginBottom: 10 }}>{title as ReactNode}</div> : null}
      <div className="rf-desc" style={cols > 1 ? { display: 'grid', gridTemplateColumns: `repeat(${cols}, 1fr)` } : undefined}>
        {children as ReactNode}
      </div>
    </div>
  );
}
DescriptionsImpl.Item = function Item({ label, children }: Record<string, unknown>) {
  return (
    <div className="rf-desc__row">
      <div className="rf-desc__label">{label as ReactNode}</div>
      <div className="rf-desc__val">{children as ReactNode}</div>
    </div>
  );
};
export const Descriptions = cast<'Descriptions'>(DescriptionsImpl);

/* ---------------------- List (+ Item / Item.Meta) ---------------------- */
function ListItemMeta({ avatar, title, description }: Record<string, unknown>) {
  return (
    <div style={{ display: 'flex', gap: 12, flex: 1, minWidth: 0 }}>
      {avatar ? <div style={{ flexShrink: 0 }}>{avatar as ReactNode}</div> : null}
      <div style={{ minWidth: 0, flex: 1 }}>
        {title != null ? <div style={{ font: '600 13.5px var(--tk-font)', color: 'var(--tk-heading)' }}>{title as ReactNode}</div> : null}
        {description != null ? <div style={{ fontSize: 12.5, color: 'var(--tk-body)', marginTop: 2 }}>{description as ReactNode}</div> : null}
      </div>
    </div>
  );
}
function ListItemImpl({ children, actions, extra, style, onClick }: Record<string, unknown>) {
  const acts = actions as ReactNode[] | undefined;
  return (
    <div className={`rf-row ${onClick ? 'rf-row--click' : ''}`} style={{ gap: 12, ...(style as CSSProperties) }} onClick={onClick as React.MouseEventHandler<HTMLDivElement> | undefined}>
      {children as ReactNode}
      {acts && acts.length ? <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexShrink: 0 }}>{acts}</div> : null}
      {extra ? <div style={{ flexShrink: 0 }}>{extra as ReactNode}</div> : null}
    </div>
  );
}
ListItemImpl.Meta = ListItemMeta;
function ListImpl<T>(props: Record<string, unknown>) {
  const data = (props.dataSource ?? []) as T[];
  const renderItem = props.renderItem as ((item: T, index: number) => ReactNode) | undefined;
  const loading = props.loading as boolean;
  const header = props.header as ReactNode;
  const footer = props.footer as ReactNode;
  const emptyText = (props.locale as { emptyText?: ReactNode } | undefined)?.emptyText;
  return (
    <div className="rf-card" style={props.style as CSSProperties}>
      {header ? <div className="rf-card__head">{header}</div> : null}
      {loading ? (
        <div style={{ padding: 24, textAlign: 'center', color: 'var(--tk-muted)' }}>Đang tải…</div>
      ) : data.length === 0 ? (
        <RfEmpty text={typeof emptyText === 'string' ? emptyText : 'Không có dữ liệu'} />
      ) : (
        data.map((it, i) => <Fragment key={i}>{renderItem?.(it, i)}</Fragment>)
      )}
      {footer ? <div style={{ padding: '10px 16px', borderTop: '1px solid var(--tk-line)' }}>{footer}</div> : null}
    </div>
  );
}
ListImpl.Item = ListItemImpl;
export const List = cast<'List'>(ListImpl);

/* ---------------------- Steps ---------------------- */
const STEP_COLOR: Record<string, string> = { wait: 'var(--tk-muted)', process: 'var(--tk-accent)', finish: 'var(--tk-success)', error: 'var(--tk-danger)' };
function StepsImpl(props: Record<string, unknown>) {
  const items = (props.items ?? []) as { title?: ReactNode; description?: ReactNode; status?: string }[];
  const current = (props.current as number) ?? 0;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      {items.map((s, i) => {
        const status = s.status ?? (i < current ? 'finish' : i === current ? 'process' : 'wait');
        const color = STEP_COLOR[status] ?? 'var(--tk-muted)';
        return (
          <div key={i} style={{ display: 'flex', gap: 12 }}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <span style={{ width: 22, height: 22, borderRadius: '50%', background: color, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', font: '700 11px var(--tk-font-mono)', flexShrink: 0 }}>
                {status === 'finish' ? <Icon name="check" size={14} /> : i + 1}
              </span>
              {i < items.length - 1 ? <span style={{ width: 2, flex: 1, minHeight: 18, background: 'var(--tk-line)' }} /> : null}
            </div>
            <div style={{ paddingBottom: 14 }}>
              <div style={{ font: '600 13px var(--tk-font)', color: 'var(--tk-heading)' }}>{s.title}</div>
              {s.description ? <div style={{ fontSize: 12.5, color: 'var(--tk-body)', marginTop: 3 }}>{s.description}</div> : null}
            </div>
          </div>
        );
      })}
    </div>
  );
}
export const Steps = cast<'Steps'>(StepsImpl);

/* ---------------------- Result ---------------------- */
const RESULT_ICON: Record<string, { icon: string; color: string }> = {
  success: { icon: 'check_circle', color: 'var(--tk-success)' },
  error: { icon: 'cancel', color: 'var(--tk-danger)' },
  warning: { icon: 'warning', color: 'var(--tk-warning)' },
  info: { icon: 'info', color: 'var(--tk-info)' },
  404: { icon: 'search_off', color: 'var(--tk-muted)' },
  403: { icon: 'lock', color: 'var(--tk-muted)' },
  500: { icon: 'error', color: 'var(--tk-danger)' },
};
function ResultImpl(props: Record<string, unknown>) {
  const { status, title, subTitle, extra, icon } = asProps(props);
  const meta = RESULT_ICON[String(status ?? 'info')] ?? RESULT_ICON.info!;
  return (
    <div style={{ textAlign: 'center', padding: '32px 16px' }}>
      {(icon as ReactNode) ?? <Icon name={meta.icon} size={56} style={{ color: meta.color }} />}
      <div style={{ font: '700 20px var(--tk-font)', color: 'var(--tk-heading)', marginTop: 12 }}>{title as ReactNode}</div>
      {subTitle ? <div style={{ fontSize: 13.5, color: 'var(--tk-muted-2)', marginTop: 6 }}>{subTitle as ReactNode}</div> : null}
      {extra ? <div style={{ marginTop: 20, display: 'flex', gap: 8, justifyContent: 'center' }}>{extra as ReactNode}</div> : null}
    </div>
  );
}
export const Result = cast<'Result'>(ResultImpl);

/* ---------------------- Table (+ Summary) ---------------------- */
function SummaryRoot({ children }: { children?: ReactNode }) {
  return <Fragment>{children}</Fragment>;
}
SummaryRoot.Row = function SRow({ children }: { children?: ReactNode }) {
  return <tr>{children}</tr>;
};
SummaryRoot.Cell = function SCell({ children, colSpan, align }: { children?: ReactNode; colSpan?: number; index?: number; align?: 'left' | 'right' | 'center' }) {
  return (
    <td colSpan={colSpan} style={{ textAlign: align }}>
      {children}
    </td>
  );
};

type AnyRec = Record<string, unknown>;
function cellValue<T>(col: ColumnType<T>, row: T): unknown {
  const di = col.dataIndex as string | string[] | number | undefined;
  if (di == null) return undefined;
  if (Array.isArray(di)) return di.reduce<unknown>((acc, k) => (acc as AnyRec)?.[k as string], row);
  return (row as AnyRec)[di as string];
}

function TableImpl<T>(props: Record<string, unknown>) {
  const columns = (props.columns ?? []) as ColumnsType<T>;
  const rows = ((props.dataSource ?? []) as T[]) ?? [];
  const rowKey = props.rowKey;
  const loading = props.loading as boolean;
  const pagination = props.pagination;
  const scroll = props.scroll as { x?: number | string } | undefined;
  const summary = props.summary as ((data: readonly T[]) => ReactNode) | undefined;
  const size = props.size;

  const flatCols = columns.flatMap((c) => ('children' in c && c.children ? (c.children as ColumnType<T>[]) : [c as ColumnType<T>]));

  const keyOf = (r: T, i: number): string => {
    if (typeof rowKey === 'function') return String((rowKey as (r: T) => React.Key)(r));
    if (typeof rowKey === 'string') return String((r as AnyRec)[rowKey] ?? i);
    return String(i);
  };

  const pag = pagination === false ? null : (pagination as { current?: number; pageSize?: number; total?: number; onChange?: (p: number, s: number) => void } | undefined);
  const minWidth = scroll && typeof scroll.x === 'number' ? scroll.x : scroll?.x ? 720 : undefined;

  return (
    <div className="rf-tablewrap">
      <table className="rf-table" style={{ minWidth }} data-size={size === 'small' ? 'sm' : undefined}>
        <thead>
          <tr>
            {flatCols.map((c, ci) => (
              <th key={c.key != null ? String(c.key) : ci} style={{ width: c.width as number, textAlign: c.align }}>
                {c.title as ReactNode}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr>
              <td colSpan={flatCols.length} style={{ padding: '40px 0', textAlign: 'center', color: 'var(--tk-muted)' }}>
                Đang tải…
              </td>
            </tr>
          ) : rows.length === 0 ? (
            <tr>
              <td colSpan={flatCols.length}>
                <RfEmpty text="Không có dữ liệu" />
              </td>
            </tr>
          ) : (
            rows.map((r, ri) => (
              <tr key={keyOf(r, ri)}>
                {flatCols.map((c, ci) => {
                  const rendered = c.render ? c.render(cellValue(c, r), r, ri) : (cellValue(c, r) as ReactNode);
                  const node = isValidElement(rendered) || typeof rendered !== 'object' ? (rendered as ReactNode) : String(rendered ?? '');
                  return (
                    <td key={c.key != null ? String(c.key) : ci} style={{ textAlign: c.align, ...(c.ellipsis ? { maxWidth: (c.width as number) ?? 220, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } : null) }}>
                      {node}
                    </td>
                  );
                })}
              </tr>
            ))
          )}
        </tbody>
        {summary && rows.length > 0 ? <tfoot>{summary(rows)}</tfoot> : null}
      </table>
      {pag ? (
        <div style={{ padding: '10px 16px' }}>
          <PaginationBar current={pag.current ?? 1} pageSize={pag.pageSize ?? 20} total={pag.total ?? rows.length} onChange={pag.onChange} />
        </div>
      ) : null}
    </div>
  );
}
TableImpl.Summary = SummaryRoot;
export const Table = cast<'Table'>(TableImpl);

function PaginationBar({ current, pageSize, total, onChange }: { current: number; pageSize: number; total: number; onChange?: (p: number, s: number) => void }) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  if (total === 0) return null;
  const from = (current - 1) * pageSize + 1;
  const to = Math.min(current * pageSize, total);
  const pages: (number | '…')[] = [];
  if (totalPages <= 7) for (let i = 1; i <= totalPages; i++) pages.push(i);
  else {
    pages.push(1);
    if (current > 3) pages.push('…');
    for (let i = Math.max(2, current - 1); i <= Math.min(totalPages - 1, current + 1); i++) pages.push(i);
    if (current < totalPages - 2) pages.push('…');
    pages.push(totalPages);
  }
  return (
    <div className="rf-pg">
      <span className="rf-pg__info">
        Hiển thị {from.toLocaleString('vi-VN')}–{to.toLocaleString('vi-VN')} trong {total.toLocaleString('vi-VN')}
      </span>
      <div className="rf-pg__list">
        <button type="button" className="rf-pg__btn" disabled={current <= 1} onClick={() => onChange?.(current - 1, pageSize)} aria-label="Trang trước">
          ‹
        </button>
        {pages.map((p, i) =>
          p === '…' ? (
            <span key={`e${i}`} className="rf-pg__info" style={{ padding: '0 4px' }}>
              …
            </span>
          ) : (
            <button key={p} type="button" className={`rf-pg__btn ${p === current ? 'rf-pg__btn--active' : ''}`} onClick={() => onChange?.(p, pageSize)}>
              {p}
            </button>
          ),
        )}
        <button type="button" className="rf-pg__btn" disabled={current >= totalPages} onClick={() => onChange?.(current + 1, pageSize)} aria-label="Trang sau">
          ›
        </button>
      </div>
    </div>
  );
}
