import { Button, Layout, Menu, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

const { Header, Sider, Content } = Layout;

type NavItem = { key: string; label: string; perm: string };

const NAV: NavItem[] = [
  { key: '/dashboard', label: 'Tổng quan', perm: 'report.dashboard.view' },
  { key: '/customers', label: 'Khách hàng', perm: 'customer.view' },
  { key: '/leads', label: 'Lead (CRM)', perm: 'lead.view' },
  { key: '/customer-cares', label: 'Chăm sóc KH', perm: 'care.view' },
  { key: '/tour-ratings', label: 'Đánh giá tour', perm: 'rating.view' },
  { key: '/tour-templates', label: 'Tour mẫu', perm: 'tour.view' },
  { key: '/departures', label: 'Chuyến đi', perm: 'departure.view' },
  { key: '/orders', label: 'Đơn hàng', perm: 'booking.view' },
  { key: '/providers', label: 'Nhà cung cấp', perm: 'provider.view' },
  { key: '/service-items', label: 'Danh mục dịch vụ', perm: 'service.view' },
  { key: '/provider-services', label: 'Bảng giá NCC', perm: 'service.view' },
  { key: '/marketing', label: 'Marketing', perm: 'marketing.view' },
  { key: '/market-types', label: 'Loại thị trường', perm: 'market.view' },
  { key: '/reports/order-debt', label: 'Công nợ', perm: 'report.debt.view' },
  { key: '/reports/provider-debt', label: 'Công nợ NCC', perm: 'report.providerdebt.view' },
  { key: '/reports/cash-flow', label: 'Dòng tiền', perm: 'report.cashflow.view' },
  { key: '/reports/turnover', label: 'Doanh thu', perm: 'report.turnover.view' },
  { key: '/reports/commission-by-user', label: 'Hoa hồng NV', perm: 'report.commission.view' },
  { key: '/commission-rules', label: 'Cấu hình hoa hồng', perm: 'commission.view' },
  { key: '/billing', label: 'Gói dịch vụ', perm: 'subscription.view' },
  { key: '/activity-logs', label: 'Nhật ký thao tác', perm: 'activitylog.view' },
];

export function AppShell() {
  const { has, email, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

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
