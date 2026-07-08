import { Space, Typography } from 'antd';
import type { ReactNode } from 'react';

export function PageHeader({ title, extra }: { title: string; extra?: ReactNode }) {
  return (
    <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
      <Typography.Title level={3} style={{ margin: 0 }}>
        {title}
      </Typography.Title>
      <Space>{extra}</Space>
    </Space>
  );
}
