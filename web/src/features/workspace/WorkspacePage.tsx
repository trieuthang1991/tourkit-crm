import { Avatar, Button, Card, Col, List, Row, Space, Table, Tabs, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  BellOutlined,
  CalendarOutlined,
  DollarOutlined,
  FileAddOutlined,
  IdcardOutlined,
  PlusOutlined,
  ReadOutlined,
  ScheduleOutlined,
  TeamOutlined,
  UserAddOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { customersCrud } from '../customers/customersCrud';
import { useNotifications } from '../notifications/api';
import { useOrderDebt } from '../reports/reportApi';
import { customerCareSchema } from '../care/customerCareTypes';
import { DepartureCalendar } from '../booking/DepartureCalendar';
import { receiptListItemSchema, paymentListItemSchema } from '../finance/listTypes';
import { postSchema } from '../posts/types';
import { workTaskSchema, priorityLabel, statusLabel } from '../workTasks/types';
import type { WorkTask } from '../workTasks/types';
import { TaskDonut } from './TaskDonut';
import type { DonutSegment } from './TaskDonut';
import { CeoAnalytics } from './CeoAnalytics';

const TASK_STATUS_META: { status: number; label: string; color: string }[] = [
  { status: 0, label: 'Cần làm', color: '#8c8c8c' },
  { status: 1, label: 'Đang làm', color: '#1677ff' },
  { status: 2, label: 'Hoàn thành', color: '#52c41a' },
  { status: 3, label: 'Huỷ', color: '#f5222d' },
];

function isToday(iso: string | null): boolean {
  if (!iso) return false;
  const d = new Date(iso);
  const n = new Date();
  return d.getFullYear() === n.getFullYear() && d.getMonth() === n.getMonth() && d.getDate() === n.getDate();
}
function isOverdue(t: WorkTask): boolean {
  if (!t.dueDate || t.status === 2 || t.status === 3) return false;
  return new Date(t.dueDate) < new Date(new Date().toDateString());
}
const dateVi = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');

export function WorkspacePage() {
  const { email, has } = useAuth();
  const navigate = useNavigate();

  const tasks = useQuery({
    queryKey: ['work-tasks', 'workspace'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/work-tasks');
      return z.array(workTaskSchema).parse(data);
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
  const pendingReceipts = useQuery({
    queryKey: ['receipts-all', 'pending'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/receipts', { params: { page: 1, size: 50 } });
      return pagedSchema(receiptListItemSchema).parse(data).items.filter((r) => r.status === 0);
    },
    enabled: has('receipt.view'),
  });
  const pendingPayments = useQuery({
    queryKey: ['payments-all', 'pending'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payments', { params: { page: 1, size: 50 } });
      return pagedSchema(paymentListItemSchema).parse(data).items.filter((p) => p.status === 0);
    },
    enabled: has('payment.view'),
  });
  const posts = useQuery({
    queryKey: ['posts', 'workspace'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/posts');
      return z.array(postSchema).parse(data);
    },
    enabled: has('post.view'),
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

  const allTasks = tasks.data ?? [];
  const overdueTasks = allTasks.filter(isOverdue);
  const doneTasks = allTasks.filter((t) => t.status === 2);

  const quickActions = [
    { label: 'Tạo việc', icon: <PlusOutlined />, to: '/work-tasks', perm: 'task.view' },
    { label: 'Tạo cơ hội', icon: <TeamOutlined />, to: '/leads', perm: 'lead.view' },
    { label: 'Tạo lịch hẹn', icon: <CalendarOutlined />, to: '/customer-cares', perm: 'care.view' },
    { label: 'Tạo Data khách', icon: <UserAddOutlined />, to: '/customers', perm: 'customer.view' },
    { label: 'Tạo đơn', icon: <FileAddOutlined />, to: '/orders', perm: 'booking.view' },
  ].filter((a) => has(a.perm));

  const taskColumns: ColumnsType<WorkTask> = [
    { title: 'Công việc', dataIndex: 'title', key: 'title', ellipsis: true },
    { title: 'Phụ trách', dataIndex: 'assigneeName', key: 'assigneeName', width: 140, render: (v: string | null) => v ?? '—' },
    { title: 'Ưu tiên', dataIndex: 'priority', key: 'priority', width: 100, render: (v: number) => priorityLabel(v) },
    { title: 'Hạn', dataIndex: 'dueDate', key: 'dueDate', width: 110, render: dateVi },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (v: number) => {
        const m = TASK_STATUS_META.find((x) => x.status === v);
        return <Tag color={m?.color}>{statusLabel(v)}</Tag>;
      },
    },
  ];

  const taskTable = (data: WorkTask[]) => (
    <Table rowKey="id" size="small" columns={taskColumns} dataSource={data} loading={tasks.isLoading} pagination={{ pageSize: 8 }} scroll={{ x: 'max-content' }} />
  );

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Card styles={{ body: { padding: 12 } }}>
        <Space wrap size={12}>
          {quickActions.map((a) => (
            <Button key={a.to} icon={a.icon} onClick={() => navigate(a.to)}>
              {a.label}
            </Button>
          ))}
        </Space>
      </Card>

      {/* CEO Analytics (bám staging "Bàn làm việc") — KPI + hiệu suất chi nhánh + top sales/KH */}
      <CeoAnalytics />

      {/* Hàng 1: hồ sơ+donut / thông báo / công nợ */}
      <Row gutter={[16, 16]}>
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

        <Col xs={24} lg={8}>
          <Card title={<Space><BellOutlined /> Thông báo bạn cần quan tâm</Space>} styles={{ body: { padding: 0, height: 340, overflow: 'auto' } }}>
            <List
              dataSource={(notifications.data ?? []).slice(0, 10)}
              loading={notifications.isLoading}
              locale={{ emptyText: 'Không có thông báo' }}
              renderItem={(n) => (
                <List.Item style={{ padding: '10px 16px', cursor: n.linkUrl ? 'pointer' : 'default' }} onClick={() => n.linkUrl && navigate(n.linkUrl)}>
                  <List.Item.Meta
                    title={<span style={{ fontWeight: n.isRead ? 400 : 600 }}>{n.title}</span>}
                    description={
                      <Space direction="vertical" size={0}>
                        <span>{n.message}</span>
                        <Typography.Text type="secondary" style={{ fontSize: 12 }}>{new Date(n.createdAt).toLocaleString('vi-VN')}</Typography.Text>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card title={<Space><DollarOutlined /> Công nợ khách hàng</Space>} styles={{ body: { padding: 0, height: 340, overflow: 'auto' } }}>
            <List
              dataSource={topDebt}
              loading={debt.isLoading}
              locale={{ emptyText: 'Không có công nợ' }}
              renderItem={(d, i) => (
                <List.Item style={{ padding: '10px 16px', cursor: 'pointer' }} onClick={() => navigate(`/orders/${d.orderId}`)}>
                  <List.Item.Meta
                    avatar={<Tag color={i < 3 ? '#EB5324' : 'default'}>#{i + 1}</Tag>}
                    title={customerName.get(d.customerId) ?? d.orderCode}
                    description={<Typography.Text type="secondary">{d.orderCode}</Typography.Text>}
                  />
                  <Typography.Text strong style={{ color: '#cf1322' }}>{money(d.outstanding)}</Typography.Text>
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>

      {/* Hàng 2: thông tin cá nhân+lịch hẹn / phiếu cần duyệt / thông tin doanh nghiệp */}
      <Row gutter={[16, 16]}>
        <Col xs={24} lg={8}>
          <Card title={<Space><IdcardOutlined /> Thông tin cá nhân</Space>}>
            <Space direction="vertical" size={10} style={{ width: '100%' }}>
              <Row justify="space-between"><Typography.Text type="secondary">Email</Typography.Text><span>{email ?? '—'}</span></Row>
              <Row justify="space-between"><Typography.Text type="secondary">Điện thoại</Typography.Text><span>—</span></Row>
              <Row justify="space-between"><Typography.Text type="secondary">Văn phòng</Typography.Text><span>—</span></Row>
            </Space>
            <div style={{ borderTop: '1px solid #f0f0f0', margin: '14px 0 10px' }} />
            <Typography.Text strong><CalendarOutlined /> Lịch hẹn hôm nay</Typography.Text>
            <List
              size="small"
              dataSource={todayCares}
              loading={cares.isLoading}
              locale={{ emptyText: 'Hôm nay không có lịch hẹn' }}
              renderItem={(c) => (
                <List.Item style={{ padding: '8px 0' }}>
                  <List.Item.Meta
                    title={c.title}
                    description={
                      <Space size={8}>
                        <Typography.Text type="secondary">{customerName.get(c.customerId) ?? ''}</Typography.Text>
                        {c.remindAt ? <Tag color="#EB5324">{new Date(c.remindAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</Tag> : null}
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card title={<Space><ScheduleOutlined /> Phiếu cần duyệt</Space>} styles={{ body: { padding: 0 } }}>
            <Tabs
              style={{ padding: '0 12px' }}
              items={[
                {
                  key: 'receipt',
                  label: `Phiếu thu (${pendingReceipts.data?.length ?? 0})`,
                  children: (
                    <List
                      size="small"
                      dataSource={pendingReceipts.data ?? []}
                      loading={pendingReceipts.isLoading}
                      locale={{ emptyText: 'Không có phiếu chờ' }}
                      style={{ maxHeight: 280, overflow: 'auto' }}
                      renderItem={(r) => (
                        <List.Item onClick={() => navigate('/receipts')} style={{ cursor: 'pointer' }}>
                          <List.Item.Meta title={r.code} description={r.customerName ?? r.orderCode ?? ''} />
                          <Space direction="vertical" align="end" size={0}>
                            <Typography.Text strong>{money(r.amount)}</Typography.Text>
                            <Tag color="green">THU</Tag>
                          </Space>
                        </List.Item>
                      )}
                    />
                  ),
                },
                {
                  key: 'payment',
                  label: `Phiếu chi (${pendingPayments.data?.length ?? 0})`,
                  children: (
                    <List
                      size="small"
                      dataSource={pendingPayments.data ?? []}
                      loading={pendingPayments.isLoading}
                      locale={{ emptyText: 'Không có phiếu chờ' }}
                      style={{ maxHeight: 280, overflow: 'auto' }}
                      renderItem={(p) => (
                        <List.Item onClick={() => navigate('/payments')} style={{ cursor: 'pointer' }}>
                          <List.Item.Meta title={p.code} description={p.providerName ?? p.orderCode ?? ''} />
                          <Space direction="vertical" align="end" size={0}>
                            <Typography.Text strong>{money(p.amount)}</Typography.Text>
                            <Tag color="red">CHI</Tag>
                          </Space>
                        </List.Item>
                      )}
                    />
                  ),
                },
              ]}
            />
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card
            title={<Space><ReadOutlined /> Thông tin doanh nghiệp</Space>}
            extra={<Button type="link" size="small" onClick={() => navigate('/posts')}>Xem thêm</Button>}
            styles={{ body: { padding: 0, height: 320, overflow: 'auto' } }}
          >
            <List
              dataSource={(posts.data ?? []).slice(0, 8)}
              loading={posts.isLoading}
              locale={{ emptyText: 'Chưa có bài viết' }}
              renderItem={(p) => (
                <List.Item style={{ padding: '10px 16px', cursor: 'pointer' }} onClick={() => navigate('/posts')}>
                  <List.Item.Meta
                    title={p.title}
                    description={
                      <Space size={8}>
                        {p.categoryName ? <Tag>{p.categoryName}</Tag> : null}
                        <Typography.Text type="secondary" style={{ fontSize: 12 }}>{p.publishedAt ? new Date(p.publishedAt).toLocaleDateString('vi-VN') : ''}</Typography.Text>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>

      {/* Lịch khởi hành: lịch tháng các chuyến (bám widget hệ cũ) */}
      {has('departure.view') ? (
        <Card
          title={<Space><CalendarOutlined /> Lịch khởi hành</Space>}
          extra={<Button type="link" onClick={() => navigate('/operations-calendar')}>Xem lịch điều hành →</Button>}
        >
          <DepartureCalendar />
        </Card>
      ) : null}

      {/* Công việc của tôi */}
      {has('task.view') ? (
        <Card title={<Space><ScheduleOutlined /> Công việc của tôi</Space>} styles={{ body: { paddingTop: 0 } }}>
          <Tabs
            items={[
              { key: 'all', label: `Tất cả (${allTasks.length})`, children: taskTable(allTasks) },
              { key: 'overdue', label: `Quá hạn (${overdueTasks.length})`, children: taskTable(overdueTasks) },
              { key: 'done', label: `Hoàn thành (${doneTasks.length})`, children: taskTable(doneTasks) },
            ]}
          />
        </Card>
      ) : null}
    </Space>
  );
}
