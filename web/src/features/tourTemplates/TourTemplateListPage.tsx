import { Alert, Button, Layout, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useAuth } from '../auth/AuthContext';
import { useTourTemplates } from './api/tourTemplateHooks';
import type { TourTemplate } from './types';

const { Header, Content } = Layout;

const columns: ColumnsType<TourTemplate> = [
  { title: 'Mã tour', dataIndex: 'code', key: 'code' },
  { title: 'Tên tour', dataIndex: 'title', key: 'title' },
  {
    title: 'Giá người lớn',
    dataIndex: 'priceAdult',
    key: 'priceAdult',
    render: (value: number) => value.toLocaleString('vi-VN'),
  },
  { title: 'Tổng số chỗ', dataIndex: 'totalSlots', key: 'totalSlots' },
];

export function TourTemplateListPage() {
  const { logout } = useAuth();
  const { data, isLoading, isError, error } = useTourTemplates();

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ color: '#fff', margin: 0 }}>
          TourKit
        </Typography.Title>
        <Button onClick={logout}>Đăng xuất</Button>
      </Header>
      <Content style={{ padding: 24 }}>
        {isError ? (
          <Alert
            type="error"
            message="Không tải được danh sách tour."
            description={error instanceof Error ? error.message : undefined}
            style={{ marginBottom: 16 }}
          />
        ) : null}
        <Table<TourTemplate> rowKey="id" columns={columns} dataSource={data ?? []} loading={isLoading} />
      </Content>
    </Layout>
  );
}
