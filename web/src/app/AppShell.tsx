import { Button, Layout, Menu, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

const { Header, Sider, Content } = Layout;

type NavItem = { key: string; label: string; perm: string };

const NAV: NavItem[] = [
  { key: '/customers', label: 'Khách hàng', perm: 'customer.view' },
  { key: '/leads', label: 'Lead (CRM)', perm: 'lead.view' },
  { key: '/tour-templates', label: 'Tour mẫu', perm: 'tour.view' },
  { key: '/departures', label: 'Chuyến đi', perm: 'departure.view' },
  { key: '/orders', label: 'Đơn hàng', perm: 'booking.view' },
  { key: '/providers', label: 'Nhà cung cấp', perm: 'provider.view' },
  { key: '/marketing', label: 'Marketing', perm: 'marketing.view' },
  { key: '/market-types', label: 'Loại thị trường', perm: 'market.view' },
  { key: '/reports/order-debt', label: 'Công nợ', perm: 'report.debt.view' },
  { key: '/billing', label: 'Gói dịch vụ', perm: 'subscription.view' },
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
