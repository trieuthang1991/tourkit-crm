import type { ReactNode } from 'react';

// Card khối nội dung chuẩn (.tk-card). Header có title + slot phải.
export function DataCard({
  title,
  extra,
  children,
  bodyPadding = false,
}: {
  title?: ReactNode;
  extra?: ReactNode;
  children: ReactNode;
  bodyPadding?: boolean;
}) {
  return (
    <div className="tk-card">
      {title || extra ? (
        <div className="tk-card__head">
          <div className="tk-card__title">{title}</div>
          {extra}
        </div>
      ) : null}
      <div style={bodyPadding ? { padding: 18 } : undefined}>{children}</div>
    </div>
  );
}

// Thanh lọc: bọc ô search + select + nút trong .tk-toolbar
export function FilterToolbar({ children }: { children: ReactNode }) {
  return <div className="tk-toolbar">{children}</div>;
}

// Avatar chữ cái tròn màu accent (.tk-avatar)
export function InitialsAvatar({ name, size = 38 }: { name: string; size?: number }) {
  const parts = name.replace(/^(Công ty|Cty)\s+(TNHH\s+)?/i, '').trim().split(/\s+/);
  const initials = ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase();
  return (
    <span className="tk-avatar" style={{ width: size, height: size, fontSize: Math.round(size * 0.36) }}>
      {initials}
    </span>
  );
}
