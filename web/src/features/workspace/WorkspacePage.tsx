import { Avatar, Button, Card, Col, List, Row, Space, Tag, Typography } from 'antd';
import {
  BellOutlined,
  CalendarOutlined,
  DollarOutlined,
  FileAddOutlined,
  PlusOutlined,
  TeamOutlined,
  UserAddOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { customersCrud } from '../customers/customersCrud';
import { useNotifications } from '../notifications/api';
import { useOrderDebt } from '../reports/reportApi';
import { customerCareSchema } from '../care/customerCareTypes';
import { TaskDonut } from './TaskDonut';
import type { DonutSegment } from './TaskDonut';

const workTaskRowSchema = z.object({ id: z.string().uuid(), title: z.string(), status: z.number() });

// Màu trạng thái công việc bám nhãn WorkTasks (0 Cần làm,1 Đang làm,2 Hoàn thành,3 Huỷ).
const TASK_STATUS_META: { status: number; label: string; color: string }[] = [
  { status: 0, label: 'Cần làm', color: '#8c8c8c' },
  { status: 1, label: 'Đang làm', color: '#1677ff' },
  { status: 2, label: 'Hoàn thành', color: '#52c41a' },
  { status: 3, label: 'Huỷ', color: '#f5222d' },
];

function isToday(iso: string | null): boolean {
  if (!iso) return false;
  const d = new Date(iso);
  const now = new Date();
  return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate();
}

export function WorkspacePage() {
  const { email, has } = useAuth();
  const navigate = useNavigate();

  const tasks = useQuery({
    queryKey: ['work-tasks', 'workspace'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/work-tasks');
      return z.array(workTaskRowSchema).parse(data);
    },
    enabled: has('task.view'),
  });

  const cares = useQuery({
    queryKey: ['customer-cares', 'workspace'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-cares');
      return z.array(customerCareSchema).parse(data);
    },
    enabled: has('care.view'),
  });

  const notifications = useNotifications();
  const debt = useOrderDebt();
  const customers = customersCrud.useList({ page: 1, size: 200 });

  const customerName = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of customers.data?.items ?? []) map.set(c.id, c.fullName);
    return map;
  }, [customers.data]);

  const donutSegments: DonutSegment[] = TASK_STATUS_META.map((m) => ({
    label: m.label,
    color: m.color,
    value: (tasks.data ?? []).filter((t) => t.status === m.status).length,
  }));

  const topDebt = useMemo(
    () => [...(debt.data ?? [])].sort((a, b) => b.outstanding - a.outstanding).slice(0, 8),
    [debt.data],
  );

  const todayCares = useMemo(() => (cares.data ?? []).filter((c) => isToday(c.remindAt)), [cares.data]);

  const quickActions = [
    { label: 'Tạo việc', icon: <PlusOutlined />, to: '/work-tasks', perm: 'task.view' },
    { label: 'Tạo cơ hội', icon: <TeamOutlined />, to: '/leads', perm: 'lead.view' },
    { label: 'Tạo lịch hẹn', icon: <CalendarOutlined />, to: '/customer-cares', perm: 'care.view' },
    { label: 'Tạo Data khách', icon: <UserAddOutlined />, to: '/customers', perm: 'customer.view' },
    { label: 'Tạo đơn', icon: <FileAddOutlined />, to: '/orders', perm: 'booking.view' },
  ].filter((a) => has(a.perm));

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {/* Thanh tạo nhanh */}
      <Card styles={{ body: { padding: 12 } }}>
        <Space wrap size={12}>
          {quickActions.map((a) => (
            <Button key={a.to} icon={a.icon} onClick={() => navigate(a.to)}>
              {a.label}
            </Button>
          ))}
        </Space>
      </Card>

      <Row gutter={[16, 16]}>
        {/* Hồ sơ + tỉ lệ công việc */}
        <Col xs={24} lg={8}>
          <Card>
            <Space direction="vertical" align="center" style={{ width: '100%' }} size={4}>
              <Avatar size={72} style={{ background: '#EB5324' }} icon={<UserOutlined />} />
              <Typography.Title level={5} style={{ margin: '8px 0 0' }}>
                {email ?? 'Người dùng'}
              </Typography.Title>
              <Tag color="#EB5324">Nhân viên</Tag>
            </Space>
            <div style={{ borderTop: '1px solid #f0f0f0', margin: '16px 0' }} />
            <Typography.Text strong>Tỉ lệ công việc</Typography.Text>
            <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginTop: 12 }}>
              <TaskDonut segments={donutSegments} />
              <Space direction="vertical" size={4}>
                {donutSegments.map((s) => (
                  <Space key={s.label} size={8}>
                    <span style={{ width: 10, height: 10, borderRadius: 2, background: s.color, display: 'inline-block' }} />
                    <Typography.Text type="secondary">
                      {s.label} ({s.value})
                    </Typography.Text>
                  </Space>
                ))}
              </Space>
            </div>
          </Card>
        </Col>

        {/* Thông báo */}
        <Col xs={24} lg={8}>
          <Card
            title={
              <Space>
                <BellOutlined /> Thông báo bạn cần quan tâm
              </Space>
            }
            styles={{ body: { padding: 0, maxHeight: 360, overflow: 'auto' } }}
          >
            <List
              dataSource={(notifications.data ?? []).slice(0, 8)}
              loading={notifications.isLoading}
              locale={{ emptyText: 'Không có thông báo' }}
              renderItem={(n) => (
                <List.Item style={{ padding: '10px 16px', cursor: n.linkUrl ? 'pointer' : 'default' }} onClick={() => n.linkUrl && navigate(n.linkUrl)}>
                  <List.Item.Meta
                    title={<span style={{ fontWeight: n.isRead ? 400 : 600 }}>{n.title}</span>}
                    description={
                      <Space direction="vertical" size={0}>
                        <span>{n.message}</span>
                        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                          {new Date(n.createdAt).toLocaleString('vi-VN')}
                        </Typography.Text>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>

        {/* Công nợ khách hàng (outstanding) */}
        <Col xs={24} lg={8}>
          <Card
            title={
              <Space>
                <DollarOutlined /> Công nợ khách hàng
              </Space>
            }
            styles={{ body: { padding: 0, maxHeight: 360, overflow: 'auto' } }}
          >
            <List
              dataSource={topDebt}
              loading={debt.isLoading}
              locale={{ emptyText: 'Không có công nợ' }}
              renderItem={(d, i) => (
                <List.Item
                  style={{ padding: '10px 16px', cursor: 'pointer' }}
                  onClick={() => navigate(`/orders/${d.orderId}`)}
                >
                  <List.Item.Meta
                    avatar={<Tag color={i < 3 ? '#EB5324' : 'default'}>#{i + 1}</Tag>}
                    title={customerName.get(d.customerId) ?? d.orderCode}
                    description={<Typography.Text type="secondary">{d.orderCode}</Typography.Text>}
                  />
                  <Typography.Text strong style={{ color: '#cf1322' }}>
                    {money(d.outstanding)}
                  </Typography.Text>
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>

      {/* Lịch hẹn hôm nay */}
      <Card
        title={
          <Space>
            <CalendarOutlined /> Lịch hẹn hôm nay
          </Space>
        }
        styles={{ body: { padding: 0 } }}
      >
        <List
          dataSource={todayCares}
          loading={cares.isLoading}
          locale={{ emptyText: 'Hôm nay không có lịch hẹn' }}
          renderItem={(c) => (
            <List.Item style={{ padding: '10px 16px' }}>
              <List.Item.Meta
                title={c.title}
                description={
                  <Space size={12}>
                    <Typography.Text type="secondary">{customerName.get(c.customerId) ?? ''}</Typography.Text>
                    {c.remindAt ? (
                      <Tag color="#EB5324">{new Date(c.remindAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</Tag>
                    ) : null}
                  </Space>
                }
              />
            </List.Item>
          )}
        />
      </Card>
    </Space>
  );
}
