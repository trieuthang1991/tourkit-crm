import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { money, moneyCompact } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { customersCrud } from '../customers/customersCrud';
import { useNotifications } from '../notifications/api';
import { useDashboard } from '../reports/dashboardApi';
import { useOrderDebt } from '../reports/reportApi';
import { customerCareSchema } from '../care/customerCareTypes';
import { receiptListItemSchema, paymentListItemSchema } from '../finance/listTypes';
import { postSchema } from '../posts/types';
import { workTaskSchema, priorityLabel, statusLabel } from '../workTasks/types';
import type { WorkTask } from '../workTasks/types';
import { TaskDonut } from './TaskDonut';
import type { DonutSegment } from './TaskDonut';
import { Button, Card, DataCard, Empty, Icon, Pill, StatGrid, Tabs } from '../../ui/kit';
import { Avatar, Divider } from '../../ui/primitives';
import { Table } from '../../ui/Table';
import type { Column } from '../../ui/Table';
import { DepartureCalendar } from '../booking/DepartureCalendar';

/* Màn "Bàn làm việc" (/workspace) — hệ Refined. KHÔNG antd, KHÔNG gradient.
   Màn RIÊNG với /dashboard (CEO Analytics). Dữ liệu/section giữ NGUYÊN. */

type ReceiptListItem = z.infer<typeof receiptListItemSchema>;
type PaymentListItem = z.infer<typeof paymentListItemSchema>;

const workTaskStatsSchema = z.object({
  total: z.number(),
  todo: z.number(),
  inProgress: z.number(),
  done: z.number(),
  cancelled: z.number(),
  overdue: z.number(),
});

const TASK_STATUS_META: { status: number; label: string; color: string; tone: 'muted' | 'info' | 'success' | 'danger' }[] = [
  { status: 0, label: 'Cần làm', color: 'var(--tk-muted)', tone: 'muted' },
  { status: 1, label: 'Đang làm', color: 'var(--tk-info)', tone: 'info' },
  { status: 2, label: 'Hoàn thành', color: 'var(--tk-success)', tone: 'success' },
  { status: 3, label: 'Huỷ', color: 'var(--tk-danger)', tone: 'danger' },
];
const toneOf = (s: number) => TASK_STATUS_META.find((m) => m.status === s)?.tone ?? 'muted';

function isToday(iso: string | null): boolean {
  if (!iso) return false;
  const d = new Date(iso);
  const n = new Date();
  return d.getFullYear() === n.getFullYear() && d.getMonth() === n.getMonth() && d.getDate() === n.getDate();
}
const dateVi = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');
const shortDateTime = (iso: string) => new Date(iso).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' });

// Dòng danh sách: avatar/số thứ tự + tiêu đề/mô tả + phần phải.
function Row({
  lead,
  title,
  desc,
  right,
  onClick,
}: {
  lead: ReactNode;
  title: ReactNode;
  desc?: ReactNode;
  right?: ReactNode;
  onClick?: () => void;
}) {
  return (
    <div onClick={onClick} className={`rf-row ${onClick ? 'rf-row--click' : ''}`}>
      {lead}
      <div className="rf-row__body">
        <div className="rf-row__top">
          <span className="rf-row__title">{title}</span>
          {right}
        </div>
        {desc ? <div className="rf-row__desc">{desc}</div> : null}
      </div>
    </div>
  );
}

export function WorkspacePage() {
  const { email, has } = useAuth();
  const navigate = useNavigate();

  // Đếm theo trạng thái lấy từ endpoint stats (KHÔNG fetch toàn bộ công việc) — dùng cho donut + badge.
  const taskStats = useQuery({
    queryKey: ['work-tasks', 'stats', 'workspace'],
    queryFn: async () => workTaskStatsSchema.parse((await httpClient.get<unknown>('/api/v1/work-tasks/stats')).data),
    enabled: has('task.view'),
  });
  // Danh sách hiển thị chỉ lấy 1 TRANG NHỎ (6 việc gần nhất), có link "Xem tất cả" sang trang đầy đủ.
  const recentTasks = useQuery({
    queryKey: ['work-tasks', 'recent', 'workspace'],
    queryFn: async () =>
      pagedSchema(workTaskSchema).parse(
        (await httpClient.get<unknown>('/api/v1/work-tasks', { params: { page: 1, size: 6 } })).data,
      ).items,
    enabled: has('task.view'),
  });
  const cares = useQuery({
    queryKey: ['customer-cares', 'workspace'],
    queryFn: async () =>
      pagedSchema(customerCareSchema).parse(
        (await httpClient.get<unknown>('/api/v1/customer-cares', { params: { page: 1, size: 50 } })).data,
      ).items,
    enabled: has('care.view'),
  });
  const pendingReceipts = useQuery({
    queryKey: ['receipts-all', 'pending'],
    queryFn: async () =>
      pagedSchema(receiptListItemSchema)
        .parse((await httpClient.get<unknown>('/api/v1/receipts', { params: { page: 1, size: 50 } })).data)
        .items.filter((r) => r.status === 0),
    enabled: has('receipt.view'),
  });
  const pendingPayments = useQuery({
    queryKey: ['payments-all', 'pending'],
    queryFn: async () =>
      pagedSchema(paymentListItemSchema)
        .parse((await httpClient.get<unknown>('/api/v1/payments', { params: { page: 1, size: 50 } })).data)
        .items.filter((p) => p.status === 0),
    enabled: has('payment.view'),
  });
  const posts = useQuery({
    queryKey: ['posts', 'workspace'],
    queryFn: async () =>
      pagedSchema(postSchema).parse(
        (await httpClient.get<unknown>('/api/v1/posts', { params: { page: 1, size: 8 } })).data,
      ).items,
    enabled: has('post.view'),
  });

  const notifications = useNotifications();
  const debt = useOrderDebt();
  const dashboard = useDashboard();
  const customers = customersCrud.useList({ page: 1, size: 200 });

  const customerName = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of customers.data?.items ?? []) map.set(c.id, c.fullName);
    return map;
  }, [customers.data]);

  const st = taskStats.data;
  const statByStatus = (s: number) =>
    s === 0 ? (st?.todo ?? 0) : s === 1 ? (st?.inProgress ?? 0) : s === 2 ? (st?.done ?? 0) : (st?.cancelled ?? 0);
  const donutSegments: DonutSegment[] = TASK_STATUS_META.map((m) => ({
    label: m.label,
    color: m.color,
    value: statByStatus(m.status),
  }));

  const topDebt = useMemo(() => [...(debt.data ?? [])].sort((a, b) => b.outstanding - a.outstanding).slice(0, 6), [debt.data]);
  const todayCares = useMemo(() => (cares.data ?? []).filter((c) => isToday(c.remindAt)), [cares.data]);

  const recent = recentTasks.data ?? [];

  const quickActions = [
    { label: 'Tạo việc', icon: 'add_task', to: '/work-tasks', perm: 'task.view' },
    { label: 'Tạo cơ hội', icon: 'my_location', to: '/leads', perm: 'lead.view' },
    { label: 'Tạo lịch hẹn', icon: 'event', to: '/customer-cares', perm: 'care.view' },
    { label: 'Tạo Data khách', icon: 'person_add', to: '/customers', perm: 'customer.view' },
    { label: 'Tạo đơn', icon: 'add_shopping_cart', to: '/orders', perm: 'booking.view' },
  ].filter((a) => has(a.perm));

  const taskColumns: Column<WorkTask>[] = [
    { key: 'title', title: 'Công việc', dataIndex: 'title' },
    { key: 'assigneeName', title: 'Phụ trách', render: (t) => t.assigneeName ?? '—' },
    { key: 'priority', title: 'Ưu tiên', render: (t) => priorityLabel(t.priority) },
    { key: 'dueDate', title: 'Hạn', mono: true, render: (t) => dateVi(t.dueDate) },
    { key: 'status', title: 'Trạng thái', render: (t) => <Pill tone={toneOf(t.status)}>{statusLabel(t.status)}</Pill> },
  ];
  const taskTable = (data: WorkTask[]) => <Table columns={taskColumns} data={data} rowKey={(t) => t.id} empty="Không có công việc" />;

  const s = dashboard.data;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div>
        <h1 className="rf-page__title">Bàn làm việc</h1>
        <div className="rf-page__sub">Chào mừng, {email ?? 'bạn'} — tổng quan công việc và hoạt động hôm nay.</div>
      </div>

      {/* Quick actions */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
        {quickActions.map((a) => (
          <Button key={a.to} icon={a.icon} onClick={() => navigate(a.to)}>
            {a.label}
          </Button>
        ))}
      </div>

      {/* KPI */}
      {has('report.dashboard.view') ? (
        <StatGrid
          items={[
            { label: 'Doanh thu (đã ghi nhận)', value: moneyCompact(s?.totalRevenue ?? 0) },
            { label: 'Đơn hàng', value: (s?.orderCount ?? 0).toLocaleString('vi-VN') },
            { label: 'Khách hàng', value: (customers.data?.total ?? 0).toLocaleString('vi-VN') },
            { label: 'Công nợ phải thu', value: moneyCompact(s?.receivableOutstanding ?? 0) },
          ]}
        />
      ) : null}

      {/* Hàng 1 */}
      <div className="rf-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))' }}>
        {/* Hồ sơ + donut */}
        <Card style={{ padding: 20 }}>
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Avatar size={68}>
              <Icon name="person" size={32} />
            </Avatar>
            <div style={{ marginTop: 12, font: '700 15.5px var(--tk-font)', color: 'var(--tk-heading)' }}>{email ?? 'Người dùng'}</div>
            <div style={{ marginTop: 6 }}>
              <Pill tone="accent">Nhân viên</Pill>
            </div>
          </div>
          <Divider />
          <div className="rf-card__title">Tỉ lệ công việc</div>
          <div style={{ marginTop: 14, display: 'flex', alignItems: 'center', gap: 18 }}>
            <TaskDonut segments={donutSegments} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 7 }}>
              {donutSegments.map((seg) => (
                <div key={seg.label} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5, color: 'var(--tk-body)' }}>
                  <span style={{ width: 9, height: 9, borderRadius: 2, background: seg.color, flexShrink: 0 }} />
                  {seg.label}
                  <span style={{ font: '600 12px var(--tk-font-mono)', color: 'var(--tk-muted-2)' }}>{seg.value}</span>
                </div>
              ))}
            </div>
          </div>
        </Card>

        {/* Thông báo */}
        <DataCard title="Thông báo bạn cần quan tâm" bodyless>
          <div style={{ height: 340, overflow: 'auto' }}>
            {notifications.data?.length ? (
              notifications.data.slice(0, 10).map((n) => (
                <Row
                  key={n.id}
                  lead={
                    <Avatar size={30} style={n.isRead ? { background: 'var(--tk-nav-hover)', color: 'var(--tk-muted-2)' } : undefined}>
                      <Icon name="notifications" size={16} />
                    </Avatar>
                  }
                  onClick={() => n.linkUrl && navigate(n.linkUrl)}
                  title={<span className={n.isRead ? undefined : 'rf-row__title--unread'}>{n.title}</span>}
                  desc={n.message}
                  right={<span className="rf-row__time">{shortDateTime(n.createdAt)}</span>}
                />
              ))
            ) : (
              <Empty text="Không có thông báo" />
            )}
          </div>
        </DataCard>

        {/* Công nợ khách hàng */}
        <DataCard title="Công nợ khách hàng" bodyless>
          <div style={{ height: 340, overflow: 'auto' }}>
            {topDebt.length ? (
              topDebt.map((d, i) => (
                <Row
                  key={d.orderId}
                  lead={
                    <Avatar size={30}>
                      <span style={{ fontFamily: 'var(--tk-font-mono)', fontSize: 12 }}>{i + 1}</span>
                    </Avatar>
                  }
                  onClick={() => navigate(`/orders/${d.orderId}`)}
                  title={customerName.get(d.customerId) ?? d.orderCode}
                  desc={d.orderCode}
                  right={<span style={{ flexShrink: 0, font: '700 13px var(--tk-font-mono)', color: 'var(--tk-danger)' }}>{money(d.outstanding)}</span>}
                />
              ))
            ) : (
              <Empty text="Không có công nợ" />
            )}
          </div>
        </DataCard>
      </div>

      {/* Hàng 2 */}
      <div className="rf-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))' }}>
        {/* Thông tin cá nhân + lịch hẹn */}
        <Card style={{ padding: 18 }}>
          <div className="rf-card__title" style={{ marginBottom: 12 }}>
            Thông tin cá nhân
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 9 }}>
            <div className="rf-kv">
              <span className="rf-kv__k">Email</span>
              <span className="rf-kv__v">{email ?? '—'}</span>
            </div>
            <div className="rf-kv">
              <span className="rf-kv__k">Điện thoại</span>
              <span className="rf-kv__v">—</span>
            </div>
            <div className="rf-kv">
              <span className="rf-kv__k">Văn phòng</span>
              <span className="rf-kv__v">—</span>
            </div>
          </div>
          <Divider />
          <div className="rf-card__title" style={{ marginBottom: 10 }}>
            Lịch hẹn hôm nay
          </div>
          {todayCares.length ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {todayCares.map((c) => (
                <div key={c.id} className="rf-kv" style={{ alignItems: 'center' }}>
                  <span className="rf-kv__v">{c.title}</span>
                  {c.remindAt ? <Pill tone="warning">{new Date(c.remindAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</Pill> : null}
                </div>
              ))}
            </div>
          ) : (
            <div style={{ fontSize: 13, color: 'var(--tk-muted)' }}>Hôm nay không có lịch hẹn</div>
          )}
        </Card>

        {/* Phiếu cần duyệt */}
        <DataCard title="Phiếu cần duyệt" bodyless>
          <PendingVouchers
            receipts={pendingReceipts.data ?? []}
            payments={pendingPayments.data ?? []}
            onReceipt={() => navigate('/receipts')}
            onPayment={() => navigate('/payments')}
          />
        </DataCard>

        {/* Thông tin doanh nghiệp */}
        <DataCard
          title="Thông tin doanh nghiệp"
          bodyless
          extra={
            <Button variant="link" onClick={() => navigate('/posts')}>
              Xem thêm
            </Button>
          }
        >
          <div style={{ height: 300, overflow: 'auto' }}>
            {posts.data?.length ? (
              posts.data.slice(0, 8).map((p) => (
                <Row
                  key={p.id}
                  lead={
                    <Avatar size={30}>
                      <Icon name="article" size={16} />
                    </Avatar>
                  }
                  onClick={() => navigate('/posts')}
                  title={p.title}
                  desc={
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                      {p.categoryName ? <Pill tone="info">{p.categoryName}</Pill> : null}
                      <span>{p.publishedAt ? new Date(p.publishedAt).toLocaleDateString('vi-VN') : ''}</span>
                    </span>
                  }
                />
              ))
            ) : (
              <Empty text="Chưa có bài viết" />
            )}
          </div>
        </DataCard>
      </div>

      {/* Lịch khởi hành */}
      {has('departure.view') ? (
        <DataCard
          title="Lịch khởi hành"
          extra={
            <Button variant="link" onClick={() => navigate('/operations-calendar')}>
              Xem lịch điều hành →
            </Button>
          }
        >
          <DepartureCalendar />
        </DataCard>
      ) : null}

      {/* Công việc của tôi — badge đếm từ stats + 6 việc gần nhất (không tải toàn bộ) */}
      {has('task.view') ? (
        <DataCard
          title="Công việc của tôi"
          extra={
            <Button variant="link" onClick={() => navigate('/work-tasks')}>
              Xem tất cả →
            </Button>
          }
        >
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
            <Pill tone="muted">Cần làm · {st?.todo ?? 0}</Pill>
            <Pill tone="info">Đang làm · {st?.inProgress ?? 0}</Pill>
            <Pill tone="success">Hoàn thành · {st?.done ?? 0}</Pill>
            <Pill tone="danger">Quá hạn · {st?.overdue ?? 0}</Pill>
          </div>
          {taskTable(recent)}
        </DataCard>
      ) : null}
    </div>
  );
}

/* Tabs phiếu thu/chi chờ duyệt (nội bộ trang) */
function PendingVouchers({
  receipts,
  payments,
  onReceipt,
  onPayment,
}: {
  receipts: ReceiptListItem[];
  payments: PaymentListItem[];
  onReceipt: () => void;
  onPayment: () => void;
}) {
  const list = (rows: { code: string; sub: string; amount: number }[], tone: 'success' | 'danger', tag: string, onClick: () => void) =>
    rows.length ? (
      <div style={{ maxHeight: 280, overflow: 'auto' }}>
        {rows.map((r) => (
          <div key={r.code} onClick={onClick} className="rf-row rf-row--click">
            <div className="rf-row__body">
              <div style={{ font: '500 13px var(--tk-font)', color: 'var(--tk-heading)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{r.code}</div>
              <div className="rf-row__desc">{r.sub}</div>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 3 }}>
              <span style={{ font: '700 13px var(--tk-font-mono)', color: 'var(--tk-heading)' }}>{money(r.amount)}</span>
              <Pill tone={tone}>{tag}</Pill>
            </div>
          </div>
        ))}
      </div>
    ) : (
      <Empty text="Không có phiếu chờ" />
    );
  return (
    <Tabs
      items={[
        {
          key: 'receipt',
          label: `Phiếu thu (${receipts.length})`,
          children: list(receipts.map((r) => ({ code: r.code, sub: r.customerName ?? r.orderCode ?? '', amount: r.amount })), 'success', 'THU', onReceipt),
        },
        {
          key: 'payment',
          label: `Phiếu chi (${payments.length})`,
          children: list(payments.map((p) => ({ code: p.code, sub: p.providerName ?? p.orderCode ?? '', amount: p.amount })), 'danger', 'CHI', onPayment),
        },
      ]}
    />
  );
}

