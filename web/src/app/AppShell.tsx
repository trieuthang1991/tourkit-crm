import { Avatar, Badge, Button, Dropdown, Layout, Menu, Tooltip, Typography } from 'antd';
import {
  BankOutlined,
  BarChartOutlined,
  BellOutlined,
  CalculatorOutlined,
  CarOutlined,
  DashboardOutlined,
  FundOutlined,
  HistoryOutlined,
  HomeOutlined,
  IdcardOutlined,
  LogoutOutlined,
  PercentageOutlined,
  ProfileOutlined,
  ProjectOutlined,
  SendOutlined,
  SettingOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  SoundOutlined,
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

// key = ĐỊNH DANH menu (duy nhất); to = route local điều hướng; children = submenu lồng.
// Bám CHÍNH XÁC menu hệ cũ (staging.tourkit.vn — HTML MenuLeft), map label/thứ tự/nhóm hệ cũ sang route local.
// Tính năng đã hợp nhất ở local → nhiều mục legacy trỏ chung 1 màn (giữ nhãn để dò 1:1). NCC là danh sách động.
type NavNode = { key: string; label: string; icon?: ReactNode; perm?: string; to?: string; children?: NavNode[] };

const MENU: NavNode[] = [
  {
    key: 'g-workspace', label: 'Workspace', icon: <DashboardOutlined />, children: [
      { key: 'w-social', label: 'Mạng Nội Bộ', to: '/posts', perm: 'post.view' },
      { key: 'w-workspace', label: 'Bàn làm việc', to: '/workspace', perm: 'report.dashboard.view' },
      { key: 'w-dashboard', label: 'Tổng quan', to: '/dashboard', perm: 'report.dashboard.view' },
      { key: 'w-noti', label: 'Thông báo', to: '/notifications', perm: 'report.dashboard.view' },
    ],
  },
  {
    key: 'g-provider', label: 'Nhà cung cấp', icon: <ShopOutlined />, children: [
      { key: 'p-all', label: 'Tất cả Nhà cung cấp', to: '/providers', perm: 'provider.view' },
      { key: 'p-services', label: 'Danh mục dịch vụ', to: '/service-items', perm: 'service.view' },
      { key: 'p-pricing', label: 'Bảng giá NCC', to: '/provider-services', perm: 'service.view' },
      { key: 'p-terms', label: 'Điều khoản TT NCC', to: '/payment-terms', perm: 'provider.view' },
      { key: 'p-series', label: 'Series Vé / Quỹ vé', to: '/ticket-funds', perm: 'ticketfund.view' },
    ],
  },
  {
    key: 'g-crm', label: 'CRM', icon: <TeamOutlined />, children: [
      { key: 'crm-share', label: 'Chia số Sale', to: '/leads', perm: 'lead.view' },
      { key: 'crm-opp', label: 'Cơ hội bán hàng', to: '/leads', perm: 'lead.view' },
      { key: 'crm-data', label: 'Data khách hàng', to: '/customers', perm: 'customer.view' },
      { key: 'crm-care', label: 'Quản lý lịch hẹn', to: '/customer-cares', perm: 'care.view' },
      {
        key: 'crm-feedback', label: 'Feedback', children: [
          { key: 'fb-general', label: 'Feedback chung', to: '/tour-ratings', perm: 'rating.view' },
          { key: 'fb-tour', label: 'Feedback theo Tour', to: '/tour-ratings', perm: 'rating.view' },
          { key: 'fb-zns', label: 'Feedback ZNS', to: '/tour-ratings', perm: 'rating.view' },
        ],
      },
    ],
  },
  {
    key: 'g-quote', label: 'Báo Giá', icon: <CalculatorOutlined />, children: [
      { key: 'q-tour', label: 'Tính giá Tour', to: '/quotes', perm: 'quote.view' },
      { key: 'q-combo', label: 'Tính giá Combo', to: '/quotes', perm: 'quote.view' },
      { key: 'q-git', label: 'Tour GIT/Combo', to: '/quotes', perm: 'quote.view' },
      { key: 'q-landtour', label: 'Landtour', to: '/quotes', perm: 'quote.view' },
      { key: 'q-booking', label: 'Booking Phòng', to: '/quotes', perm: 'quote.view' },
      { key: 'q-service', label: 'Dịch vụ lẻ', to: '/quotes', perm: 'quote.view' },
      { key: 'q-visa', label: 'Visa', to: '/quotes', perm: 'quote.view' },
      { key: 'q-agent', label: 'Báo giá Đại lý (B2B)', to: '/agent-quotes', perm: 'agentquote.view' },
    ],
  },
  {
    key: 'g-order', label: 'Đơn hàng/LKH', icon: <ShoppingCartOutlined />, children: [
      { key: 'o-all', label: 'Tất cả đơn hàng', to: '/orders', perm: 'booking.view' },
      { key: 'o-tours', label: 'Tất cả Tour/LKH', to: '/departures', perm: 'departure.view' },
      { key: 'o-fit', label: 'Tour FIT', to: '/departures', perm: 'departure.view' },
      { key: 'o-git', label: 'Tour GIT/Combo', to: '/departures', perm: 'departure.view' },
      { key: 'o-landtour', label: 'LandTour', to: '/departures', perm: 'departure.view' },
      { key: 'o-visa', label: 'Visa', to: '/orders', perm: 'booking.view' },
      { key: 'o-service', label: 'Dịch vụ lẻ', to: '/orders', perm: 'booking.view' },
    ],
  },
  {
    key: 'g-booking', label: 'Booking Phòng/Khách sạn', icon: <HomeOutlined />, children: [
      { key: 'b-roomfund', label: 'Quỹ phòng', to: '/room-classes', perm: 'servicebooking.view' },
      { key: 'b-list', label: 'Danh sách Booking', to: '/service-bookings', perm: 'servicebooking.view' },
    ],
  },
  {
    key: 'g-flight', label: 'Vé Máy Bay', icon: <SendOutlined />, children: [
      { key: 'f-provider', label: 'Nhà cung cấp vé', to: '/providers', perm: 'provider.view' },
      { key: 'f-group', label: 'Vé máy bay đoàn', to: '/flight-tickets', perm: 'ticketfund.view' },
      { key: 'f-individual', label: 'Vé máy bay lẻ', to: '/flight-tickets', perm: 'ticketfund.view' },
    ],
  },
  {
    key: 'g-guide', label: 'Hướng dẫn viên', icon: <IdcardOutlined />, children: [
      { key: 'gd-provider', label: 'Hướng dẫn viên', to: '/guide-assignments', perm: 'guide.view' },
      { key: 'gd-calendar', label: 'Lịch điều Hướng dẫn viên', to: '/guide-assignments', perm: 'guide.view' },
      { key: 'gd-report', label: 'Báo cáo', to: '/guide-assignments', perm: 'guide.view' },
    ],
  },
  {
    key: 'g-vehicle', label: 'Quản lý xe', icon: <CarOutlined />, children: [
      { key: 'v-store', label: 'Kho xe', to: '/vehicles', perm: 'vehicle.view' },
      { key: 'v-waiting', label: 'Lịch xe chờ duyệt', to: '/vehicle-assignments', perm: 'vehicle.view' },
      { key: 'v-manage', label: 'Lịch điều xe', to: '/vehicle-assignments', perm: 'vehicle.view' },
      { key: 'v-report', label: 'Báo cáo', to: '/vehicle-assignments', perm: 'vehicle.view' },
    ],
  },
  {
    key: 'g-operation', label: 'Điều hành Tour', icon: <ProfileOutlined />, children: [
      { key: 'op-voucher', label: 'Phiếu điều hành dịch vụ', to: '/service-operations', perm: 'servicebooking.view' },
      { key: 'op-calendar', label: 'Lịch điều hành', to: '/operations-calendar', perm: 'departure.view' },
    ],
  },
  {
    key: 'g-finance', label: 'Tài chính/Kế toán', icon: <BankOutlined />, children: [
      { key: 'fi-waiting', label: 'Phiếu thu chờ', to: '/receipts', perm: 'receipt.view' },
      { key: 'fi-receipt', label: 'Phiếu thu', to: '/receipts', perm: 'receipt.view' },
      { key: 'fi-payment', label: 'Phiếu chi', to: '/payments', perm: 'payment.view' },
      { key: 'fi-invoice', label: 'Danh sách hoá đơn (VAT)', to: '/invoices', perm: 'invoice.view' },
      { key: 'fi-cashflow', label: 'Thống kê dòng tiền', to: '/reports/cash-flow', perm: 'report.cashflow.view' },
      { key: 'fi-debt-c', label: 'Công nợ khách', to: '/reports/order-debt', perm: 'report.debt.view' },
      { key: 'fi-debt-p', label: 'Công nợ NCC', to: '/reports/provider-debt', perm: 'report.providerdebt.view' },
    ],
  },
  {
    key: 'g-kpi', label: 'KPIs', icon: <FundOutlined />, children: [
      { key: 'kpi-config', label: 'Thiết lập KPIs', to: '/reports/kpi', perm: 'report.dashboard.view' },
    ],
  },
  {
    key: 'g-commission', label: 'Hoa Hồng', icon: <PercentageOutlined />, children: [
      { key: 'hh-config', label: 'Thiết lập hoa hồng', to: '/commission-rules', perm: 'commission.view' },
      { key: 'hh-customer', label: 'HH theo loại khách', to: '/customer-commission-rules', perm: 'commission.view' },
      { key: 'hh-source', label: 'Báo cáo theo nguồn', to: '/reports/commission-by-user', perm: 'report.commission.view' },
      { key: 'hh-milestone', label: 'Báo cáo theo cột mốc', to: '/reports/commission-by-user', perm: 'report.commission.view' },
    ],
  },
  {
    key: 'g-project', label: 'Dự án & Công việc', icon: <ProjectOutlined />, children: [
      { key: 'pj-project', label: 'Dự án', to: '/workflows', perm: 'workflow.view' },
      { key: 'pj-mytask', label: 'Công việc của tôi', to: '/work-tasks', perm: 'task.view' },
      { key: 'pj-tasks', label: 'Danh sách Công việc', to: '/work-tasks', perm: 'task.view' },
      { key: 'pj-perf', label: 'Báo cáo Hiệu suất', to: '/work-tasks', perm: 'task.view' },
    ],
  },
  {
    key: 'g-marketing', label: 'Marketing', icon: <SoundOutlined />, children: [
      {
        key: 'mkt-email', label: 'Email Marketing', children: [
          { key: 'mkt-campaign', label: 'Chiến dịch', to: '/marketing', perm: 'marketing.view' },
          { key: 'mkt-store', label: 'Kho Email Mẫu', to: '/message-templates', perm: 'marketing.view' },
        ],
      },
      {
        key: 'mkt-zalo', label: 'Zalo OA/ZBS', children: [
          { key: 'zalo-oa', label: 'Thông tin OA', to: '/marketing', perm: 'marketing.view' },
          { key: 'zalo-zns', label: 'ZNS', to: '/marketing', perm: 'marketing.view' },
          { key: 'zalo-uid', label: 'Zalo UID (Tin follow OA)', to: '/marketing', perm: 'marketing.view' },
        ],
      },
      { key: 'mkt-posts', label: 'Bài viết', to: '/posts', perm: 'post.view' },
      { key: 'mkt-postcat', label: 'Chuyên mục bài viết', to: '/post-categories', perm: 'post.view' },
    ],
  },
  {
    key: 'g-report', label: 'Báo cáo', icon: <BarChartOutlined />, children: [
      { key: 'rp-seller', label: 'Nhân viên', to: '/reports/turnover', perm: 'report.turnover.view' },
      { key: 'rp-money', label: 'Tài chính', to: '/reports/turnover-by-department', perm: 'report.turnover.view' },
      { key: 'rp-export', label: 'Xuất báo cáo', to: '/reports/turnover', perm: 'report.turnover.view' },
      { key: 'rp-system', label: 'Báo cáo tổng hợp', to: '/reports/turnover-by-department', perm: 'report.turnover.view' },
    ],
  },
  {
    key: 'g-agent', label: 'Đại lý (B2B)', icon: <TeamOutlined />, children: [
      { key: 'ag-list', label: 'Danh sách đại lý', to: '/agents', perm: 'agent.view' },
      { key: 'ag-booking', label: 'Đặt chỗ đại lý', to: '/agent-bookings', perm: 'agentquote.view' },
    ],
  },
  {
    key: 'g-system', label: 'Cài đặt hệ thống', icon: <SettingOutlined />, children: [
      { key: 'sys-users', label: 'Thành viên', to: '/users', perm: 'user.view' },
      { key: 'sys-config', label: 'Cấu hình', to: '/config-hub', perm: 'user.view' },
      { key: 'sys-billing', label: 'Gói dịch vụ', to: '/billing', perm: 'subscription.view' },
    ],
  },
  {
    key: 'g-log', label: 'Log hệ thống', icon: <HistoryOutlined />, children: [
      { key: 'log-system', label: 'Log hệ thống', to: '/activity-logs', perm: 'activitylog.view' },
    ],
  },
];

function flattenLeaves(nodes: NavNode[]): NavNode[] {
  return nodes.flatMap((n) => (n.children ? flattenLeaves(n.children) : [n]));
}
const LEAVES = flattenLeaves(MENU).filter((n) => n.to);
const KEY_TO_ROUTE: Record<string, string> = Object.fromEntries(LEAVES.map((l) => [l.key, l.to!]));

function findSelected(pathname: string): NavNode | undefined {
  return LEAVES
    .filter((l) => pathname === l.to || pathname.startsWith(l.to + '/') || pathname.startsWith(l.to!))
    .sort((a, b) => b.to!.length - a.to!.length)[0];
}

// Chuỗi key tổ tiên của 1 leaf (để mở đúng submenu lồng khi active).
function ancestorKeys(nodes: NavNode[], targetKey: string, trail: string[] = []): string[] | null {
  for (const n of nodes) {
    if (n.key === targetKey) return trail;
    if (n.children) {
      const r = ancestorKeys(n.children, targetKey, [...trail, n.key]);
      if (r) return r;
    }
  }
  return null;
}

function buildItems(nodes: NavNode[], has: (p: string) => boolean): NonNullable<MenuProps['items']> {
  return nodes
    .map((n) => {
      if (n.children) {
        const kids = buildItems(n.children, has);
        return kids.length ? { key: n.key, label: n.label, icon: n.icon, children: kids } : null;
      }
      if (n.perm && !has(n.perm)) return null;
      return { key: n.key, label: n.label, icon: n.icon };
    })
    .filter(Boolean) as NonNullable<MenuProps['items']>;
}

export function AppShell() {
  const { has, email, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const unread = useUnreadCount();
  const [collapsed, setCollapsed] = useState(false);

  const items = useMemo(() => buildItems(MENU, has), [has]);

  const selected = findSelected(location.pathname);
  const initialOpen = selected ? ancestorKeys(MENU, selected.key) ?? [] : ['g-workspace'];
  const [openKeys, setOpenKeys] = useState<string[]>(initialOpen);

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
          onClick={({ key }) => {
            const route = KEY_TO_ROUTE[key];
            if (route) navigate(route);
          }}
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
