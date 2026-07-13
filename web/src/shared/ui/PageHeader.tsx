import type { ReactNode } from 'react';

// Thay PageHeader inline hiện tại. Dùng class .tk-page__* (styles/components.css).
export function PageHeader({
  title,
  crumb,
  desc,
  extra,
}: {
  title: string;
  crumb?: ReactNode;   // vd: <>CRM <span>/</span> Data khách hàng</>
  desc?: string;
  extra?: ReactNode;   // nút hành động bên phải
}) {
  return (
    <div className="tk-page__head">
      <div>
        {crumb ? <div className="tk-page__crumb">{crumb}</div> : null}
        <h1 className="tk-page__title">{title}</h1>
        {desc ? <div className="tk-page__desc">{desc}</div> : null}
      </div>
      {extra ? <div style={{ display: 'flex', gap: 10 }}>{extra}</div> : null}
    </div>
  );
}
