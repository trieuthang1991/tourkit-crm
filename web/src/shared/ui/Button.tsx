import type { ButtonProps as AntButtonProps } from 'antd';
import type { CSSProperties, ReactNode } from 'react';

// Nút chuẩn TourKit — hệ "Refined" (.rf-btn), KHÔNG còn antd runtime.
// Giữ type props từ antd (import type, bị xoá khi build) để 21 màn không phải sửa.
type Variant = 'primary' | 'ghost' | 'text' | 'danger';

// Omit 'type'|'danger'|'variant' của antd, thêm variant (design system) + leftIcon.
export type ButtonProps = Omit<AntButtonProps, 'type' | 'danger' | 'variant'> & {
  variant?: Variant;
  leftIcon?: ReactNode;
};

export function Button({ variant = 'primary', leftIcon, children, ...rest }: ButtonProps) {
  const r = rest as {
    icon?: ReactNode;
    size?: 'small' | 'middle' | 'large';
    loading?: boolean;
    disabled?: boolean;
    block?: boolean;
    href?: string;
    htmlType?: 'button' | 'submit' | 'reset';
    onClick?: React.MouseEventHandler<HTMLElement>;
    style?: CSSProperties;
    className?: string;
  };
  const cls = `rf-btn rf-btn--${variant} ${r.size === 'small' ? 'rf-btn--sm' : ''} ${r.className ?? ''}`;
  const st: CSSProperties = { ...(r.block ? { width: '100%' } : null), ...r.style };
  const content = (
    <>
      {r.loading ? <span className="rf-spin" /> : (r.icon ?? leftIcon)}
      {children}
    </>
  );
  if (r.href) {
    return (
      <a className={cls} href={r.href} style={st} onClick={r.onClick}>
        {content}
      </a>
    );
  }
  return (
    <button type={r.htmlType === 'submit' ? 'submit' : r.htmlType === 'reset' ? 'reset' : 'button'} className={cls} style={st} disabled={r.disabled || r.loading} onClick={r.onClick}>
      {content}
    </button>
  );
}
