import { useState } from 'react';
import { Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { dateText } from '../../shared/format';
import { useActivityLogs } from './activityLogsApi';
import type { ActivityLog } from './activityLogsApi';

const ACTION_COLOR: Record<string, string> = {
  Insert: 'green',
  Update: 'blue',
  Delete: 'red',
};

export function ActivityLogsPage() {
  const [page, setPage] = useState(1);
  const size = 20;
  const query = useActivityLogs(page, size);

  const columns: ColumnsType<ActivityLog> = [
    { title: 'Thời gian', dataIndex: 'createdAt', key: 'createdAt', render: (v: string) => dateText(v) },
    {
      title: 'Thao tác',
      dataIndex: 'action',
      key: 'action',
      render: (v: string) => <Tag color={ACTION_COLOR[v]}>{v}</Tag>,
    },
    { title: 'Đối tượng', dataIndex: 'entityName', key: 'entityName' },
    { title: 'Mã bản ghi', dataIndex: 'entityId', key: 'entityId' },
    { title: 'User', dataIndex: 'userId', key: 'userId' },
  ];

  return (
    <>
      <Typography.Title level={3}>Nhật ký thao tác</Typography.Title>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={query.data?.items ?? []}
        loading={query.isLoading}
        pagination={{
          current: page,
          pageSize: size,
          total: query.data?.total ?? 0,
          onChange: setPage,
          showSizeChanger: false,
        }}
      />
    </>
  );
}
