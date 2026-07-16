import {
  BankOutlined,
  BellOutlined,
  CalendarOutlined,
  DollarOutlined,
  FileAddOutlined,
  FundOutlined,
  IdcardOutlined,
  PlusOutlined,
  ReadOutlined,
  ScheduleOutlined,
  ShoppingCartOutlined,
  TeamOutlined,
  UserAddOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
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
import { Btn, Card, CardHead, Empty, GradAvatar, GradientStat, Pill, Tabs } from '../../ui/kit';
import { DepartureCalendarLite } from '../../ui/DepartureCalendarLite';

type ReceiptListItem = z.infer<typeof receiptListItemSchema>;
type PaymentListItem = z.infer<typeof paymentListItemSchema>;

const TASK_STATUS_META: { status: number; label: string; color: string; tone: 'muted' | 'info' | 'success' | 'danger' }[] = [
  { status: 0, label: 'Cần làm', color: '#8c8c8c', tone: 'muted' },
  { status: 1, label: 'Đang làm', color: '#1677ff', tone: 'info' },
  { status: 2, label: 'Hoàn thành', color: '#52c41a', tone: 'success' },
  { status: 3, label: 'Huỷ', color: '#f5222d', tone: 'danger' },
];
const toneOf = (s: number) => TASK_STATUS_META.find((m) => m.status === s)?.tone ?? 'muted';

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
const shortDateTime = (iso: string) =>
  new Date(iso).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' });

// Dòng danh sách có avatar gradient + tiêu đề/mô tả + phần phải.
function Row({
  i,
  icon,
  title,
  desc,
  right,
  onClick,
}: {
  i: number;
  icon: ReactNode;
  title: ReactNode;
  desc?: ReactNode;
  right?: ReactNode;
  onClick?: () => void;
}) {
  return (
    <div
      onClick={onClick}
      className={`flex items-center gap-3 px-4 py-[11px] border-b border-[#f5f4f8] last:border-0 ${onClick ? 'cursor-pointer hover:bg-[#faf9fc]' : ''}`}
    >
      <GradAvatar i={i}>{icon}</GradAvatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline justify-between gap-2">
          <span className="truncate text-[14px] text-[#5e5873]">{title}</span>
          {right}
        </div>
        {desc ? <div className="truncate text-[13px] text-[#a8a5b5]">{desc}</div> : null}
      </div>
    </div>
  );
}

export function WorkspacePage() {
  const { email, has } = useAuth();
  const navigate = useNavigate();

  const tasks = useQuery({
    queryKey: ['work-tasks', 'workspace'],
    queryFn: async () => z.array(workTaskSchema).parse((await httpClient.get<unknown>('/api/v1/work-tasks')).data),
    enabled: has('task.view'),
  });
  const cares = useQuery({
    queryKey: ['customer-cares', 'workspace'],
    queryFn: async () => z.array(customerCareSchema).parse((await httpClient.get<unknown>('/api/v1/customer-cares')).data),
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
    queryFn: async () => z.array(postSchema).parse((await httpClient.get<unknown>('/api/v1/posts')).data),
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

  const donutSegments: DonutSegment[] = TASK_STATUS_META.map((m) => ({
    label: m.label,
    color: m.color,
    value: (tasks.data ?? []).filter((t) => t.status === m.status).length,
  }));

  const topDebt = useMemo(() => [...(debt.data ?? [])].sort((a, b) => b.outstanding - a.outstanding).slice(0, 6), [debt.data]);
  const todayCares = useMemo(() => (cares.data ?? []).filter((c) => isToday(c.remindAt)), [cares.data]);

  const allTasks = tasks.data ?? [];
  const overdueTasks = allTasks.filter(isOverdue);
  const doneTasks = allTasks.filter((t) => t.status === 2);

  const quickActions = [
    { label: 'Tạo việc', icon: <PlusOutlined />, to: '/work-tasks', perm: 'task.view', color: '#eb5324' },
    { label: 'Tạo cơ hội', icon: <TeamOutlined />, to: '/leads', perm: 'lead.view', color: '#4e7bff' },
    { label: 'Tạo lịch hẹn', icon: <CalendarOutlined />, to: '/customer-cares', perm: 'care.view', color: '#22c55e' },
    { label: 'Tạo Data khách', icon: <UserAddOutlined />, to: '/customers', perm: 'customer.view', color: '#a855f7' },
    { label: 'Tạo đơn', icon: <FileAddOutlined />, to: '/orders', perm: 'booking.view', color: '#ec4899' },
  ].filter((a) => has(a.perm));

  const taskTable = (data: WorkTask[]) => (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-[13px]">
        <thead>
          <tr className="border-b border-[#f1eff5] text-[11px] uppercase tracking-wide text-[#a8a5b5]">
            <th className="px-4 py-2.5 font-medium">Công việc</th>
            <th className="px-4 py-2.5 font-medium">Phụ trách</th>
            <th className="px-4 py-2.5 font-medium">Ưu tiên</th>
            <th className="px-4 py-2.5 font-medium">Hạn</th>
            <th className="px-4 py-2.5 font-medium">Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          {data.length === 0 ? (
            <tr>
              <td colSpan={5}>
                <Empty text="Không có công việc" />
              </td>
            </tr>
          ) : (
            data.map((t) => (
              <tr key={t.id} className="border-b border-[#f5f4f8] text-[#6e6b7b] last:border-0 hover:bg-[#faf9fc]">
                <td className="px-4 py-3 text-[#5e5873]">{t.title}</td>
                <td className="px-4 py-3">{t.assigneeName ?? '—'}</td>
                <td className="px-4 py-3">{priorityLabel(t.priority)}</td>
                <td className="px-4 py-3 font-mono">{dateVi(t.dueDate)}</td>
                <td className="px-4 py-3">
                  <Pill tone={toneOf(t.status)}>{statusLabel(t.status)}</Pill>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );

  const s = dashboard.data;

  return (
    <div className="flex flex-col gap-4 font-sans">
      {/* Header */}
      <div>
        <h1 className="m-0 text-[24px] font-bold tracking-[-0.02em] text-[#5e5873]">Bàn làm việc</h1>
        <p className="mt-1 text-[14px] text-[#a8a5b5]">Chào mừng, {email ?? 'bạn'} — tổng quan công việc và hoạt động hôm nay.</p>
      </div>

      {/* Quick actions */}
      <Card className="p-3">
        <div className="flex flex-wrap gap-3">
          {quickActions.map((a) => (
            <Btn key={a.to} onClick={() => navigate(a.to)}>
              <span style={{ color: a.color }}>{a.icon}</span>
              {a.label}
            </Btn>
          ))}
        </div>
      </Card>

      {/* KPI gradient */}
      {has('report.dashboard.view') ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <GradientStat gradient="orange" icon={<FundOutlined />} value={money(s?.totalRevenue ?? 0)} label="Doanh thu (đã ghi nhận)" />
          <GradientStat gradient="blue" icon={<ShoppingCartOutlined />} value={(s?.orderCount ?? 0).toLocaleString('vi-VN')} label="Đơn hàng" />
          <GradientStat gradient="green" icon={<TeamOutlined />} value={(customers.data?.total ?? 0).toLocaleString('vi-VN')} label="Khách hàng" />
          <GradientStat gradient="purple" icon={<BankOutlined />} value={money(s?.receivableOutstanding ?? 0)} label="Công nợ phải thu" />
        </div>
      ) : null}

      {/* Hàng 1 */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        {/* Hồ sơ + donut */}
        <Card className="p-5">
          <div className="flex flex-col items-center">
            <GradAvatar i={0} size={72}>
              <UserOutlined />
            </GradAvatar>
            <div className="mt-3 text-[16px] font-semibold text-[#5e5873]">{email ?? 'Người dùng'}</div>
            <span className="mt-1 rounded-full px-2.5 py-0.5 text-[12px] font-medium text-white" style={{ background: 'linear-gradient(135deg,#eb5324,#ff7a45)' }}>
              Nhân viên
            </span>
          </div>
          <div className="my-4 border-t border-[#f1eff5]" />
          <div className="text-[14px] font-semibold text-[#5e5873]">Tỉ lệ công việc</div>
          <div className="mt-3 flex items-center gap-4">
            <TaskDonut segments={donutSegments} />
            <div className="flex flex-col gap-1.5">
              {donutSegments.map((seg) => (
                <div key={seg.label} className="flex items-center gap-2 text-[13px] text-[#8b899a]">
                  <span className="inline-block h-2.5 w-2.5 rounded-sm" style={{ background: seg.color }} />
                  {seg.label} ({seg.value})
                </div>
              ))}
            </div>
          </div>
        </Card>

        {/* Thông báo */}
        <Card>
          <CardHead icon={<BellOutlined className="text-[#eb5324]" />} title="Thông báo bạn cần quan tâm" />
          <div className="h-[340px] overflow-auto">
            {notifications.data?.length ? (
              notifications.data.slice(0, 10).map((n, i) => (
                <Row
                  key={n.id}
                  i={i}
                  icon={<BellOutlined className="text-[15px]" />}
                  onClick={() => n.linkUrl && navigate(n.linkUrl)}
                  title={<span className={n.isRead ? '' : 'font-semibold'}>{n.title}</span>}
                  desc={n.message}
                  right={<span className="shrink-0 font-mono text-[11.5px] text-[#a8a5b5]">{shortDateTime(n.createdAt)}</span>}
                />
              ))
            ) : (
              <Empty text="Không có thông báo" />
            )}
          </div>
        </Card>

        {/* Công nợ khách hàng */}
        <Card>
          <CardHead icon={<DollarOutlined className="text-[#eb5324]" />} title="Công nợ khách hàng" />
          <div className="h-[340px] overflow-auto">
            {topDebt.length ? (
              topDebt.map((d, i) => (
                <Row
                  key={d.orderId}
                  i={i}
                  icon={i + 1}
                  onClick={() => navigate(`/orders/${d.orderId}`)}
                  title={customerName.get(d.customerId) ?? d.orderCode}
                  desc={d.orderCode}
                  right={<span className="shrink-0 font-mono text-[14px] font-semibold text-[#cf1322]">{money(d.outstanding)}</span>}
                />
              ))
            ) : (
              <Empty text="Không có công nợ" />
            )}
          </div>
        </Card>
      </div>

      {/* Hàng 2 */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        {/* Thông tin cá nhân + lịch hẹn */}
        <Card className="p-5">
          <div className="mb-3 flex items-center gap-2 text-[15px] font-semibold text-[#5e5873]">
            <IdcardOutlined className="text-[#eb5324]" /> Thông tin cá nhân
          </div>
          <div className="flex flex-col gap-2.5 text-[13px]">
            <div className="flex justify-between"><span className="text-[#a8a5b5]">Email</span><span className="text-[#5e5873]">{email ?? '—'}</span></div>
            <div className="flex justify-between"><span className="text-[#a8a5b5]">Điện thoại</span><span>—</span></div>
            <div className="flex justify-between"><span className="text-[#a8a5b5]">Văn phòng</span><span>—</span></div>
          </div>
          <div className="my-3 border-t border-[#f1eff5]" />
          <div className="mb-2 flex items-center gap-2 text-[14px] font-semibold text-[#5e5873]">
            <CalendarOutlined className="text-[#eb5324]" /> Lịch hẹn hôm nay
          </div>
          {todayCares.length ? (
            <div className="flex flex-col gap-2">
              {todayCares.map((c) => (
                <div key={c.id} className="flex items-center justify-between text-[13px]">
                  <span className="text-[#5e5873]">{c.title}</span>
                  {c.remindAt ? (
                    <Pill tone="warning">{new Date(c.remindAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</Pill>
                  ) : null}
                </div>
              ))}
            </div>
          ) : (
            <div className="text-[13px] text-[#a8a5b5]">Hôm nay không có lịch hẹn</div>
          )}
        </Card>

        {/* Phiếu cần duyệt */}
        <Card>
          <CardHead icon={<ScheduleOutlined className="text-[#eb5324]" />} title="Phiếu cần duyệt" />
          <Tabs2
            receipts={pendingReceipts.data ?? []}
            payments={pendingPayments.data ?? []}
            onReceipt={() => navigate('/receipts')}
            onPayment={() => navigate('/payments')}
          />
        </Card>

        {/* Thông tin doanh nghiệp */}
        <Card>
          <CardHead
            icon={<ReadOutlined className="text-[#eb5324]" />}
            title="Thông tin doanh nghiệp"
            extra={<Btn variant="link" onClick={() => navigate('/posts')}>Xem thêm</Btn>}
          />
          <div className="h-[300px] overflow-auto">
            {posts.data?.length ? (
              posts.data.slice(0, 8).map((p, i) => (
                <Row
                  key={p.id}
                  i={i}
                  icon={<ReadOutlined className="text-[14px]" />}
                  onClick={() => navigate('/posts')}
                  title={p.title}
                  desc={
                    <span className="flex items-center gap-2">
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
        </Card>
      </div>

      {/* Lịch khởi hành */}
      {has('departure.view') ? (
        <Card>
          <CardHead
            icon={<CalendarOutlined className="text-[#eb5324]" />}
            title="Lịch khởi hành"
            extra={<Btn variant="link" onClick={() => navigate('/operations-calendar')}>Xem lịch điều hành →</Btn>}
          />
          <DepartureCalendarLite />
        </Card>
      ) : null}

      {/* Công việc của tôi */}
      {has('task.view') ? (
        <Card>
          <CardHead icon={<ScheduleOutlined className="text-[#eb5324]" />} title="Công việc của tôi" />
          <TasksTabs all={allTasks} overdue={overdueTasks} done={doneTasks} render={taskTable} />
        </Card>
      ) : null}
    </div>
  );
}

/* Tabs phiếu thu/chi (nội bộ trang) */
function Tabs2({
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
  const list = (
    rows: { code: string; sub: string; amount: number }[],
    tone: 'success' | 'danger',
    tag: string,
    onClick: () => void,
  ) =>
    rows.length ? (
      <div className="max-h-[280px] overflow-auto">
        {rows.map((r) => (
          <div key={r.code} onClick={onClick} className="flex cursor-pointer items-center justify-between border-b border-[#f5f4f8] px-4 py-2.5 last:border-0 hover:bg-[#faf9fc]">
            <div className="min-w-0">
              <div className="truncate text-[13px] font-medium text-[#5e5873]">{r.code}</div>
              <div className="truncate text-[12px] text-[#a8a5b5]">{r.sub}</div>
            </div>
            <div className="flex flex-col items-end">
              <span className="font-mono text-[13px] font-semibold text-[#5e5873]">{money(r.amount)}</span>
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

function TasksTabs({
  all,
  overdue,
  done,
  render,
}: {
  all: WorkTask[];
  overdue: WorkTask[];
  done: WorkTask[];
  render: (d: WorkTask[]) => ReactNode;
}) {
  return (
    <Tabs
      items={[
        { key: 'all', label: `Tất cả (${all.length})`, children: render(all) },
        { key: 'overdue', label: `Quá hạn (${overdue.length})`, children: render(overdue) },
        { key: 'done', label: `Hoàn thành (${done.length})`, children: render(done) },
      ]}
    />
  );
}
