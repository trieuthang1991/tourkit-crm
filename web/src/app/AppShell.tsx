import { Avatar, Badge, Button, Dropdown, Layout, Menu, Tooltip, Typography } from 'antd';
import {
  BankOutlined,
  BarChartOutlined,
  BellOutlined,
  CalculatorOutlined,
  CarOutlined,
  ClusterOutlined,
  DashboardOutlined,
  EnvironmentOutlined,
  LogoutOutlined,
  PercentageOutlined,
  ProjectOutlined,
  SettingOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  SoundOutlined,
  TagsOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';
import type { ReactNode } from 'react';
import { useMemo, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { useUnreadCount } from '../features/notifications/api';

const { Header, Sider, Content } = Layout;

type NavLeaf = { key: string; label: string; perm: string };
type NavGroup = { key: string; label: string; icon: ReactNode; children: NavLeaf[] };

// Gom nhóm bám menu hệ cũ (KojiCRM MenuLeft.ascx): Bàn làm việc / CRM / Báo giá /
// Sản phẩm Tour / Đơn hàng / Điều xe & HDV / NCC / Tài chính / Hoa hồng / Dự án /
// Marketing / Báo cáo / Danh mục / Đại lý B2B / Cài đặt.
const GROUPS: NavGroup[] = [
  {
    key: 'g-workspace',
    label: 'Bàn làm việc',
    icon: <DashboardOutlined />,
    children: [
      { key: '/dashboard', label: 'Tổng quan', perm: 'report.dashboard.view' },
      { key: '/reports/kpi', label: 'KPI phễu', perm: 'report.dashboard.view' },
    ],
  },
  {
    key: 'g-crm',
    label: 'CRM',
    icon: <TeamOutlined />,
    children: [
      { key: '/leads', label: 'Cơ hội bán hàng (Lead)', perm: 'lead.view' },
      { key: '/customers', label: 'Khách hàng', perm: 'customer.view' },
      { key: '/customer-cares', label: 'Chăm sóc KH', perm: 'care.view' },
      { key: '/tour-ratings', label: 'Đánh giá tour', perm: 'rating.view' },
    ],
  },
  {
    key: 'g-quote',
    label: 'Báo giá',
    icon: <CalculatorOutlined />,
    children: [
      { key: '/quotes', label: 'Báo giá / Tính giá tour', perm: 'quote.view' },
      { key: '/agent-quotes', label: 'Báo giá Đại lý (B2B)', perm: 'agentquote.view' },
    ],
  },
  {
    key: 'g-product',
    label: 'Sản phẩm Tour',
    icon: <EnvironmentOutlined />,
    children: [
      { key: '/tour-templates', label: 'Tour mẫu', perm: 'tour.view' },
      { key: '/departures', label: 'Chuyến đi (LKH)', perm: 'departure.view' },
      { key: '/service-bookings', label: 'Đặt dịch vụ lẻ', perm: 'servicebooking.view' },
      { key: '/room-classes', label: 'Hạng phòng KS', perm: 'servicebooking.view' },
    ],
  },
  {
    key: 'g-order',
    label: 'Đơn hàng / LKH',
    icon: <ShoppingCartOutlined />,
    children: [
      { key: '/orders', label: 'Tất cả đơn hàng', perm: 'booking.view' },
      { key: '/operations-calendar', label: 'Lịch điều hành', perm: 'departure.view' },
      { key: '/surcharges', label: 'Loại phụ thu', perm: 'booking.view' },
      { key: '/transfer-reasons', label: 'Lý do chuyển chuyến', perm: 'booking.view' },
    ],
  },
  {
    key: 'g-fleet',
    label: 'Điều xe & HDV',
    icon: <CarOutlined />,
    children: [
      { key: '/vehicles', label: 'Kho xe', perm: 'vehicle.view' },
      { key: '/car-types', label: 'Loại xe', perm: 'vehicle.view' },
      { key: '/vehicle-assignments', label: 'Lịch điều xe', perm: 'vehicle.view' },
      { key: '/guide-assignments', label: 'Phân công HDV', perm: 'guide.view' },
      { key: '/language-types', label: 'Ngôn ngữ HDV', perm: 'guide.view' },
    ],
  },
  {
    key: 'g-provider',
    label: 'Nhà cung cấp',
    icon: <ShopOutlined />,
    children: [
      { key: '/providers', label: 'Tất cả NCC', perm: 'provider.view' },
      { key: '/service-items', label: 'Danh mục dịch vụ', perm: 'service.view' },
      { key: '/provider-services', label: 'Bảng giá NCC', perm: 'service.view' },
      { key: '/payment-terms', label: 'Điều khoản TT NCC', perm: 'provider.view' },
      { key: '/currencies', label: 'Tỷ giá tiền tệ', perm: 'service.view' },
    ],
  },
  {
    key: 'g-finance',
    label: 'Tài chính / Kế toán',
    icon: <BankOutlined />,
    children: [
      { key: '/receipts', label: 'Phiếu thu', perm: 'receipt.view' },
      { key: '/payments', label: 'Phiếu chi', perm: 'payment.view' },
      { key: '/reports/order-debt', label: 'Công nợ khách', perm: 'report.debt.view' },
      { key: '/reports/provider-debt', label: 'Công nợ NCC', perm: 'report.providerdebt.view' },
      { key: '/invoices', label: 'Hoá đơn (VAT)', perm: 'invoice.view' },
      { key: '/reports/cash-flow', label: 'Thống kê dòng tiền', perm: 'report.cashflow.view' },
      { key: '/payment-accounts', label: 'Tài khoản nhận tiền', perm: 'paymentaccount.view' },
      { key: '/ticket-funds', label: 'Quỹ vé ứng', perm: 'ticketfund.view' },
    ],
  },
  {
    key: 'g-commission',
    label: 'Hoa hồng & KPI',
    icon: <PercentageOutlined />,
    children: [
      { key: '/commission-rules', label: 'Thiết lập hoa hồng', perm: 'commission.view' },
      { key: '/customer-commission-rules', label: 'HH theo loại khách', perm: 'commission.view' },
      { key: '/reports/commission-by-user', label: 'Hoa hồng nhân viên', perm: 'report.commission.view' },
    ],
  },
  {
    key: 'g-project',
    label: 'Dự án & Công việc',
    icon: <ProjectOutlined />,
    children: [
      { key: '/work-tasks', label: 'Danh sách công việc', perm: 'task.view' },
      { key: '/workflows', label: 'Board Kanban', perm: 'workflow.view' },
      { key: '/approval-processes', label: 'Quy trình duyệt', perm: 'approvalprocess.view' },
    ],
  },
  {
    key: 'g-marketing',
    label: 'Marketing',
    icon: <SoundOutlined />,
    children: [
      { key: '/marketing', label: 'Chiến dịch', perm: 'marketing.view' },
      { key: '/message-templates', label: 'Kho tin nhắn mẫu', perm: 'marketing.view' },
      { key: '/posts', label: 'Bài viết', perm: 'post.view' },
      { key: '/post-categories', label: 'Chuyên mục bài viết', perm: 'post.view' },
    ],
  },
  {
    key: 'g-report',
    label: 'Báo cáo',
    icon: <BarChartOutlined />,
    children: [
      { key: '/reports/turnover', label: 'Doanh thu', perm: 'report.turnover.view' },
      { key: '/reports/turnover-by-department', label: 'Doanh thu theo phòng ban', perm: 'report.turnover.view' },
    ],
  },
  {
    key: 'g-catalog',
    label: 'Danh mục',
    icon: <TagsOutlined />,
    children: [
      { key: '/customer-types', label: 'Loại khách hàng', perm: 'customertype.view' },
      { key: '/customer-sources', label: 'Nguồn khách hàng', perm: 'customertype.view' },
      { key: '/customer-tags', label: 'Nhãn khách hàng', perm: 'customertype.view' },
      { key: '/market-types', label: 'Loại thị trường', perm: 'market.view' },
    ],
  },
  {
    key: 'g-agent',
    label: 'Đại lý (B2B)',
    icon: <ClusterOutlined />,
    children: [
      { key: '/agents', label: 'Danh sách đại lý', perm: 'agent.view' },
      { key: '/agent-bookings', label: 'Đặt chỗ đại lý', perm: 'agentquote.view' },
    ],
  },
  {
    key: 'g-system',
    label: 'Cài đặt hệ thống',
    icon: <SettingOutlined />,
    children: [
      { key: '/users', label: 'Thành viên', perm: 'user.view' },
      { key: '/departments', label: 'Phòng ban', perm: 'user.view' },
      { key: '/positions', label: 'Chức vụ', perm: 'user.view' },
      { key: '/company-profile', label: 'Hồ sơ công ty', perm: 'company.manage' },
      { key: '/billing', label: 'Gói dịch vụ', perm: 'subscription.view' },
      { key: '/activity-logs', label: 'Nhật ký thao tác', perm: 'activitylog.view' },
    ],
  },
];

const ALL_LEAVES = GROUPS.flatMap((g) => g.children);

function findSelected(pathname: string): NavLeaf | undefined {
  // Chọn leaf có key khớp tiền tố DÀI NHẤT (tránh /reports/turnover nuốt /reports/turnover-by-department).
  return ALL_LEAVES
    .filter((l) => pathname === l.key || pathname.startsWith(l.key + '/') || pathname.startsWith(l.key))
    .sort((a, b) => b.key.length - a.key.length)[0];
}

export function AppShell() {
  const { has, email, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const unread = useUnreadCount();
  const [collapsed, setCollapsed] = useState(false);

  // Chỉ giữ nhóm có ít nhất 1 mục con được phép; bỏ nhóm rỗng.
  const items: MenuProps['items'] = useMemo(
    () =>
      GROUPS.map((g) => {
        const children = g.children.filter((c) => has(c.perm));
        return children.length
          ? { key: g.key, label: g.label, icon: g.icon, children: children.map((c) => ({ key: c.key, label: c.label })) }
          : null;
      }).filter(Boolean) as MenuProps['items'],
    [has],
  );

  const selected = findSelected(location.pathname);
  const selectedGroup = GROUPS.find((g) => g.children.some((c) => c.key === selected?.key));
  const [openKeys, setOpenKeys] = useState<string[]>(selectedGroup ? [selectedGroup.key] : ['g-workspace']);

  const title = selected?.label ?? 'TourKit';

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        theme="dark"
        width={248}
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        trigger={null}
        style={{ overflow: 'auto', height: '100vh', position: 'sticky', top: 0, left: 0, background: '#333333' }}
      >
        <div
          style={{
            height: 56,
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            padding: collapsed ? '0 20px' : '0 20px',
            color: '#fff',
          }}
        >
          <div
            style={{
              width: 30,
              height: 30,
              borderRadius: 8,
              background: 'linear-gradient(135deg,#EB5324,#c73e17)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 700,
              flex: '0 0 auto',
            }}
          >
            T
          </div>
          {!collapsed && (
            <Typography.Text strong style={{ color: '#fff', fontSize: 16 }}>
              TourKit
            </Typography.Text>
          )}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={selected ? [selected.key] : []}
          openKeys={collapsed ? undefined : openKeys}
          onOpenChange={(keys) => setOpenKeys(keys)}
          items={items}
          onClick={({ key }) => navigate(key)}
          style={{ borderInlineEnd: 'none' }}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            background: '#fff',
            padding: '0 20px',
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            borderBottom: '1px solid #f0f0f0',
            position: 'sticky',
            top: 0,
            zIndex: 10,
          }}
        >
          <Button
            type="text"
            aria-label={collapsed ? 'Mở rộng menu' : 'Thu gọn menu'}
            onClick={() => setCollapsed((v) => !v)}
            style={{ fontSize: 16 }}
          >
            {collapsed ? '☰' : '⟨'}
          </Button>
          <Typography.Title level={5} style={{ margin: 0, flex: 1 }}>
            {title}
          </Typography.Title>
          <Tooltip title="Thông báo">
            <Badge count={unread.data ?? 0} size="small">
              <Button type="text" icon={<BellOutlined />} onClick={() => navigate('/notifications')} aria-label="Thông báo" />
            </Badge>
          </Tooltip>
          <Dropdown
            menu={{
              items: [
                { key: 'email', label: email ?? '', disabled: true },
                { type: 'divider' },
                { key: 'logout', label: 'Đăng xuất', icon: <LogoutOutlined />, onClick: logout },
              ],
            }}
          >
            <Button type="text" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Avatar size="small" style={{ background: '#EB5324' }} icon={<UserOutlined />} />
              <span style={{ maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis' }}>{email}</span>
            </Button>
          </Dropdown>
        </Header>
        <Content style={{ padding: 24, background: '#f5f6f8' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
