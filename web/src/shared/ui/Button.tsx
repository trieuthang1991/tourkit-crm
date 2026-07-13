import { Button as AntButton } from 'antd';
import type { ButtonProps as AntButtonProps } from 'antd';
import type { ReactNode } from 'react';

// Nút chuẩn TourKit — bọc AntD, biến thể theo design system.
// variant: primary (đỏ) | ghost (viền) | text | danger
type Variant = 'primary' | 'ghost' | 'text' | 'danger';

// Omit cả 'variant' vì AntD v5 đã có prop variant riêng (union khác) — nếu không sẽ giao thành 'text'.
export type ButtonProps = Omit<AntButtonProps, 'type' | 'danger' | 'variant'> & {
  variant?: Variant;
  leftIcon?: ReactNode;
};

const map: Record<Variant, Partial<AntButtonProps>> = {
  primary: { type: 'primary' },
  ghost: { type: 'default' },
  text: { type: 'text' },
  danger: { type: 'default', danger: true },
};

export function Button({ variant = 'primary', leftIcon, children, ...rest }: ButtonProps) {
  return (
    <AntButton {...map[variant]} {...rest}>
      {leftIcon ? <span style={{ marginInlineEnd: 6 }}>{leftIcon}</span> : null}
      {children}
    </AntButton>
  );
}
