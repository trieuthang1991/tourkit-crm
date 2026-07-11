import { Badge, Button, Layout, Menu, Typography } from 'antd';
import { BellOutlined } from '@ant-design/icons';
import type { MenuProps } from 'antd';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { useUnreadCount } from '../features/notifications/api';

const { Header, Sider, Content } = Layout;

type NavItem = { key: string; label: string; perm: string };

const NAV: NavItem[] = [
  { key: '/dashboard', label: 'Tổng quan', perm: 'report.dashboard.view' },
  { key: '/reports/kpi', label: 'KPI phễu', perm: 'report.dashboard.view' },
  { key: '/customers', label: 'Khách hàng', perm: 'customer.view' },
  { key: '/customer-types', label: 'Loại khách hàng', perm: 'customertype.view' },
  { key: '/customer-sources', label: 'Nguồn khách hàng', perm: 'customertype.view' },
  { key: '/customer-tags', label: 'Nhãn khách hàng', perm: 'customertype.view' },
  { key: '/payment-accounts', label: 'Tài khoản nhận tiền', perm: 'paymentaccount.view' },
  { key: '/leads', label: 'Lead (CRM)', perm: 'lead.view' },
  { key: '/customer-cares', label: 'Chăm sóc KH', perm: 'care.view' },
  { key: '/work-tasks', label: 'Công việc', perm: 'task.view' },
  { key: '/workflows', label: 'Board Kanban', perm: 'workflow.view' },
  { key: '/approval-processes', label: 'Quy trình duyệt', perm: 'approvalprocess.view' },
  { key: '/tour-ratings', label: 'Đánh giá tour', perm: 'rating.view' },
  { key: '/tour-templates', label: 'Tour mẫu', perm: 'tour.view' },
  { key: '/departures', label: 'Chuyến đi', perm: 'departure.view' },
  { key: '/operations-calendar', label: 'Lịch điều hành', perm: 'departure.view' },
  { key: '/orders', label: 'Đơn hàng', perm: 'booking.view' },
  { key: '/surcharges', label: 'Loại phụ thu', perm: 'booking.view' },
  { key: '/vehicles', label: 'Xe', perm: 'vehicle.view' },
  { key: '/car-types', label: 'Loại xe', perm: 'vehicle.view' },
  { key: '/guide-assignments', label: 'Phân công HDV', perm: 'guide.view' },
  { key: '/language-types', label: 'Ngôn ngữ HDV', perm: 'guide.view' },
  { key: '/vehicle-assignments', label: 'Phân xe cho chuyến', perm: 'vehicle.view' },
  { key: '/service-bookings', label: 'Đặt dịch vụ lẻ', perm: 'servicebooking.view' },
  { key: '/room-classes', label: 'Hạng phòng KS', perm: 'servicebooking.view' },
  { key: '/agents', label: 'Đại lý (B2B)', perm: 'agent.view' },
  { key: '/customer-commission-rules', label: 'HH theo loại khách', perm: 'commission.view' },
  { key: '/quotes', label: 'Báo giá', perm: 'quote.view' },
  { key: '/invoices', label: 'Hoá đơn VAT', perm: 'invoice.view' },
  { key: '/agent-quotes', label: 'Báo giá Đại lý (B2B)', perm: 'agentquote.view' },
  { key: '/ticket-funds', label: 'Quỹ vé ứng', perm: 'ticketfund.view' },
  { key: '/agent-bookings', label: 'Đặt chỗ Đại lý (B2B)', perm: 'agentquote.view' },
  { key: '/providers', label: 'Nhà cung cấp', perm: 'provider.view' },
  { key: '/service-items', label: 'Danh mục dịch vụ', perm: 'service.view' },
  { key: '/provider-services', label: 'Bảng giá NCC', perm: 'service.view' },
  { key: '/currencies', label: 'Tỷ giá tiền tệ', perm: 'service.view' },
  { key: '/payment-terms', label: 'Điều khoản TT NCC', perm: 'provider.view' },
  { key: '/marketing', label: 'Marketing', perm: 'marketing.view' },
  { key: '/message-templates', label: 'Mẫu tin nhắn', perm: 'marketing.view' },
  { key: '/market-types', label: 'Loại thị trường', perm: 'market.view' },
  { key: '/posts', label: 'Bài viết', perm: 'post.view' },
  { key: '/post-categories', label: 'Chuyên mục bài viết', perm: 'post.view' },
  { key: '/reports/order-debt', label: 'Công nợ', perm: 'report.debt.view' },
  { key: '/reports/provider-debt', label: 'Công nợ NCC', perm: 'report.providerdebt.view' },
  { key: '/reports/cash-flow', label: 'Dòng tiền', perm: 'report.cashflow.view' },
  { key: '/reports/turnover', label: 'Doanh thu', perm: 'report.turnover.view' },
  { key: '/reports/turnover-by-department', label: 'Doanh thu theo phòng ban', perm: 'report.turnover.view' },
  { key: '/reports/commission-by-user', label: 'Hoa hồng NV', perm: 'report.commission.view' },
  { key: '/commission-rules', label: 'Cấu hình hoa hồng', perm: 'commission.view' },
  { key: '/users', label: 'Người dùng', perm: 'user.view' },
  { key: '/departments', label: 'Phòng ban', perm: 'user.view' },
  { key: '/positions', label: 'Chức vụ', perm: 'user.view' },
  { key: '/billing', label: 'Gói dịch vụ', perm: 'subscription.view' },
  { key: '/activity-logs', label: 'Nhật ký thao tác', perm: 'activitylog.view' },
];

export function AppShell() {
  const { has, email, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const unread = useUnreadCount();

  const items: MenuProps['items'] = NAV.filter((n) => has(n.perm)).map((n) => ({ key: n.key, label: n.label }));

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider theme="light" width={220}>
        <div style={{ padding: 16 }}>
          <Typography.Title level={4} style={{ margin: 0 }}>
            TourKit
          </Typography.Title>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[NAV.find((n) => location.pathname.startsWith(n.key))?.key ?? '']}
          items={items}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 12 }}>
          <Badge count={unread.data ?? 0} size="small">
            <Button type="text" icon={<BellOutlined />} onClick={() => navigate('/notifications')} aria-label="Thông báo" />
          </Badge>
          <span>{email}</span>
          <Button onClick={logout}>Đăng xuất</Button>
        </Header>
        <Content style={{ padding: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
