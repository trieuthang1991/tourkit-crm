import type { ReactNode } from 'react';

// Header trang DÙNG CHUNG (.tk-page__*): tiêu đề + mô tả + slot nút.
// Breadcrumb do AppShell render 1 lần cho MỌI trang (không phụ thuộc trang có dùng PageHeader hay không).
export function PageHeader({
  title,
  desc,
  extra,
}: {
  title: string;
  desc?: string;
  extra?: ReactNode;
}) {
  return (
    <div className="tk-page__head">
      <div>
        <h1 className="tk-page__title">{title}</h1>
        {desc ? <div className="tk-page__desc">{desc}</div> : null}
      </div>
      {extra ? <div style={{ display: 'flex', gap: 10 }}>{extra}</div> : null}
    </div>
  );
}
