import { useMemo, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { ErrorBoundary } from './ErrorBoundary';
import { useAuth } from '../features/auth/AuthContext';
import { useUnreadCount } from '../features/notifications/api';
import { Button, Icon, IconButton } from '../ui/kit';
import { Avatar, Badge, Tooltip } from '../ui/primitives';
import { SearchInput } from '../ui/inputs';
import { Dropdown } from '../ui/overlay';

/* =========================================================================
   AppShell hệ "Refined": rail SÁNG 252px + topbar 60px, icon Material Symbols.
   KHÔNG antd. Cấu trúc MENU giữ NGUYÊN (bám hệ cũ) — chỉ đổi lớp trình bày.
   ========================================================================= */

// key = ĐỊNH DANH menu (duy nhất); to = route local điều hướng; children = submenu lồng.
// Bám CHÍNH XÁC menu hệ cũ (staging.tourkit.vn — HTML MenuLeft), map label/thứ tự/nhóm hệ cũ sang route local.
// Tính năng đã hợp nhất ở local → nhiều mục legacy trỏ chung 1 màn (giữ nhãn để dò 1:1). NCC là danh sách động.
export type NavNode = { key: string; label: string; icon?: string; perm?: string; to?: string; children?: NavNode[] };

export const MENU: NavNode[] = [
  {
    key: 'g-workspace', label: 'Workspace', icon: 'dashboard', children: [
      { key: 'w-social', label: 'Mạng Nội Bộ', to: '/posts', perm: 'post.view' },
      { key: 'w-workspace', label: 'Bàn làm việc', to: '/workspace', perm: 'report.dashboard.view' },
      { key: 'w-dashboard', label: 'Tổng quan', to: '/dashboard', perm: 'report.dashboard.view' },
      { key: 'w-noti', label: 'Thông báo', to: '/notifications', perm: 'report.dashboard.view' },
    ],
  },
  {
    key: 'g-provider', label: 'Nhà cung cấp', icon: 'storefront', children: [
      { key: 'p-all', label: 'Tất cả Nhà cung cấp', to: '/providers', perm: 'provider.view' },
      { key: 'p-services', label: 'Danh mục dịch vụ', to: '/service-items', perm: 'service.view' },
      { key: 'p-pricing', label: 'Bảng giá NCC', to: '/provider-services', perm: 'service.view' },
      { key: 'p-terms', label: 'Điều khoản TT NCC', to: '/payment-terms', perm: 'provider.view' },
      { key: 'p-series', label: 'Series Vé / Quỹ vé', to: '/ticket-funds', perm: 'ticketfund.view' },
    ],
  },
  {
    key: 'g-crm', label: 'CRM', icon: 'groups', children: [
      { key: 'crm-share', label: 'Chia số Sale', to: '/lead-campaigns', perm: 'lead.view' },
      { key: 'crm-opp', label: 'Cơ hội bán hàng', to: '/leads', perm: 'lead.view' },
      { key: 'crm-data', label: 'Data khách hàng', to: '/customers', perm: 'customer.view' },
      { key: 'crm-dedup', label: 'Rà khách trùng', to: '/customers/duplicates', perm: 'customer.view' },
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
    key: 'g-quote', label: 'Báo Giá', icon: 'calculate', children: [
      { key: 'q-tour', label: 'Tính giá Tour', to: '/quotes', perm: 'quote.view' },
      { key: 'q-combo', label: 'Tính giá Combo', to: '/quotes/combo', perm: 'quote.view' },
      { key: 'q-git', label: 'Tour GIT/Combo', to: '/quotes/git', perm: 'quote.view' },
      { key: 'q-landtour', label: 'Landtour', to: '/quotes/landtour', perm: 'quote.view' },
      { key: 'q-booking', label: 'Booking Phòng', to: '/quotes/room-booking', perm: 'quote.view' },
      { key: 'q-service', label: 'Dịch vụ lẻ', to: '/quotes/service', perm: 'quote.view' },
      { key: 'q-visa', label: 'Visa', to: '/quotes/visa', perm: 'quote.view' },
      { key: 'q-agent', label: 'Báo giá Đại lý (B2B)', to: '/agent-quotes', perm: 'agentquote.view' },
    ],
  },
  {
    key: 'g-order', label: 'Đơn hàng/LKH', icon: 'shopping_cart', children: [
      { key: 'o-all', label: 'Tất cả đơn hàng', to: '/orders', perm: 'booking.view' },
      { key: 'o-tours', label: 'Tất cả Tour/LKH', to: '/departures', perm: 'departure.view' },
      { key: 'o-fit', label: 'Tour FIT', to: '/orders?bookingType=0', perm: 'booking.view' },
      { key: 'o-git', label: 'Tour GIT/Combo', to: '/orders?bookingType=1', perm: 'booking.view' },
      { key: 'o-landtour', label: 'LandTour', to: '/orders?bookingType=2', perm: 'booking.view' },
      { key: 'o-visa', label: 'Visa', to: '/orders?bookingType=5', perm: 'booking.view' },
      { key: 'o-service', label: 'Dịch vụ lẻ', to: '/orders?bookingType=4', perm: 'booking.view' },
    ],
  },
  {
    key: 'g-booking', label: 'Booking Phòng/Khách sạn', icon: 'hotel', children: [
      { key: 'b-roomfund', label: 'Quỹ phòng', to: '/room-fund', perm: 'roomfund.view' },
      { key: 'b-list', label: 'Danh sách Booking', to: '/service-bookings', perm: 'servicebooking.view' },
      { key: 'b-roomclass', label: 'Hạng phòng (danh mục)', to: '/room-classes', perm: 'servicebooking.view' },
    ],
  },
  {
    key: 'g-flight', label: 'Vé Máy Bay', icon: 'flight', children: [
      { key: 'f-provider', label: 'Nhà cung cấp vé', to: '/providers', perm: 'provider.view' },
      { key: 'f-group', label: 'Vé máy bay đoàn', to: '/flight-tickets', perm: 'ticketfund.view' },
      { key: 'f-individual', label: 'Vé máy bay lẻ', to: '/flight-tickets-individual', perm: 'ticketfund.view' },
    ],
  },
  {
    key: 'g-guide', label: 'Hướng dẫn viên', icon: 'badge', children: [
      { key: 'gd-provider', label: 'Hướng dẫn viên', to: '/guide-assignments', perm: 'guide.view' },
      { key: 'gd-calendar', label: 'Lịch điều Hướng dẫn viên', to: '/guide-schedule', perm: 'guide.view' },
      { key: 'gd-report', label: 'Báo cáo', to: '/guide-assignments', perm: 'guide.view' },
    ],
  },
  {
    key: 'g-vehicle', label: 'Quản lý xe', icon: 'directions_car', children: [
      { key: 'v-store', label: 'Kho xe', to: '/vehicles', perm: 'vehicle.view' },
      { key: 'v-waiting', label: 'Lịch xe chờ duyệt', to: '/vehicle-assignments', perm: 'vehicle.view' },
      { key: 'v-manage', label: 'Lịch điều xe', to: '/vehicle-schedule', perm: 'vehicle.view' },
      { key: 'v-report', label: 'Báo cáo', to: '/vehicle-assignments', perm: 'vehicle.view' },
    ],
  },
  {
    key: 'g-operation', label: 'Điều hành Tour', icon: 'assignment', children: [
      { key: 'op-voucher', label: 'Phiếu điều hành dịch vụ', to: '/service-operations', perm: 'servicebooking.view' },
      { key: 'op-calendar', label: 'Lịch điều hành', to: '/operations-calendar', perm: 'departure.view' },
    ],
  },
  {
    key: 'g-finance', label: 'Tài chính/Kế toán', icon: 'account_balance', children: [
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
    key: 'g-kpi', label: 'KPIs', icon: 'trending_up', children: [
      { key: 'kpi-config', label: 'Thiết lập KPIs', to: '/reports/kpi', perm: 'report.dashboard.view' },
    ],
  },
  {
    key: 'g-commission', label: 'Hoa Hồng', icon: 'percent', children: [
      { key: 'hh-config', label: 'Thiết lập hoa hồng', to: '/commission-rules', perm: 'commission.view' },
      { key: 'hh-campaign', label: 'Chính sách hoa hồng (bậc thang)', to: '/commission-campaigns', perm: 'commission.view' },
      { key: 'hh-customer', label: 'HH theo loại khách', to: '/customer-commission-rules', perm: 'commission.view' },
      { key: 'hh-source', label: 'Báo cáo theo nguồn', to: '/reports/commission-by-user', perm: 'report.commission.view' },
      { key: 'hh-milestone', label: 'Báo cáo theo cột mốc', to: '/reports/commission-by-milestone', perm: 'report.commission.view' },
    ],
  },
  {
    key: 'g-project', label: 'Dự án & Công việc', icon: 'checklist', children: [
      { key: 'pj-project', label: 'Dự án', to: '/workflows', perm: 'workflow.view' },
      { key: 'pj-mytask', label: 'Công việc của tôi', to: '/work-tasks', perm: 'task.view' },
      { key: 'pj-tasks', label: 'Danh sách Công việc', to: '/work-tasks', perm: 'task.view' },
      { key: 'pj-perf', label: 'Báo cáo Hiệu suất', to: '/work-tasks', perm: 'task.view' },
    ],
  },
  {
    key: 'g-marketing', label: 'Marketing', icon: 'campaign', children: [
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
    key: 'g-report', label: 'Báo cáo', icon: 'bar_chart', children: [
      { key: 'rp-seller', label: 'Nhân viên', to: '/reports/turnover', perm: 'report.turnover.view' },
      { key: 'rp-money', label: 'Tài chính', to: '/reports/turnover-by-department', perm: 'report.turnover.view' },
      { key: 'rp-tourtype', label: 'Thu chi theo loại tour', to: '/reports/money-by-tour-type', perm: 'report.turnover.view' },
      { key: 'rp-export', label: 'Xuất báo cáo', to: '/reports/turnover', perm: 'report.turnover.view' },
      { key: 'rp-system', label: 'Báo cáo tổng hợp', to: '/reports/turnover-by-department', perm: 'report.turnover.view' },
    ],
  },
  {
    key: 'g-agent', label: 'Đại lý (B2B)', icon: 'handshake', children: [
      { key: 'ag-list', label: 'Danh sách đại lý', to: '/agents', perm: 'agent.view' },
      { key: 'ag-booking', label: 'Đặt chỗ đại lý', to: '/agent-bookings', perm: 'agentquote.view' },
    ],
  },
  {
    key: 'g-system', label: 'Cài đặt hệ thống', icon: 'settings', children: [
      { key: 'sys-users', label: 'Thành viên', to: '/users', perm: 'user.view' },
      { key: 'sys-roles', label: 'Vai trò & quyền', to: '/roles', perm: 'user.view' },
      { key: 'sys-config', label: 'Cấu hình', to: '/config-hub', perm: 'user.view' },
      { key: 'sys-billing', label: 'Gói dịch vụ', to: '/billing', perm: 'subscription.view' },
    ],
  },
  {
    key: 'g-log', label: 'Log hệ thống', icon: 'history', children: [
      { key: 'log-system', label: 'Log hệ thống', to: '/activity-logs', perm: 'activitylog.view' },
    ],
  },
];

function flattenLeaves(nodes: NavNode[]): NavNode[] {
  return nodes.flatMap((n) => (n.children ? flattenLeaves(n.children) : [n]));
}
const LEAVES = flattenLeaves(MENU).filter((n) => n.to);

// Breadcrumb "Nhóm › Trang" cho route hiện tại — render 1 lần ở shell cho MỌI trang.
function crumbFor(pathname: string, fullPath: string): { group: string; page: string } | null {
  const leaf = findSelected(pathname, fullPath);
  if (!leaf) return null;
  const trail = ancestorKeys(MENU, leaf.key) ?? [];
  const group = MENU.find((g) => g.key === trail[0]);
  return { group: group?.label ?? '', page: leaf.label };
}

function AutoBreadcrumb({ pathname, fullPath }: { pathname: string; fullPath: string }) {
  const c = crumbFor(pathname, fullPath);
  if (!c || !c.group) return null;
  return (
    <div className="rf-crumb">
      <span>{c.group}</span>
      <Icon name="chevron_right" size={14} className="rf-crumb__sep" />
      <span className="rf-crumb__cur">{c.page}</span>
    </div>
  );
}

function findSelected(pathname: string, fullPath: string = pathname): NavNode | undefined {
  return LEAVES
    .filter((l) =>
      l.to!.includes('?')
        ? fullPath === l.to // leaf có query (?bookingType=) → khớp cả query
        : pathname === l.to || pathname.startsWith(l.to + '/'))
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

/** Lọc menu theo quyền: bỏ leaf thiếu perm, bỏ nhóm rỗng sau khi lọc. */
function filterByPerm(nodes: NavNode[], has: (p: string) => boolean): NavNode[] {
  return nodes
    .map((n) => {
      if (n.children) {
        const kids = filterByPerm(n.children, has);
        return kids.length ? { ...n, children: kids } : null;
      }
      return n.perm && !has(n.perm) ? null : n;
    })
    .filter(Boolean) as NavNode[];
}

function NavItems({
  nodes,
  depth,
  selectedKey,
  openKeys,
  toggle,
  go,
}: {
  nodes: NavNode[];
  depth: number;
  selectedKey?: string;
  openKeys: string[];
  toggle: (k: string) => void;
  go: (to: string) => void;
}) {
  return (
    <>
      {nodes.map((n) => {
        if (n.children) {
          const open = openKeys.includes(n.key);
          return (
            <div key={n.key}>
              <button type="button" className="rf-nav__item" onClick={() => toggle(n.key)} aria-expanded={open}>
                {n.icon ? <Icon name={n.icon} size={19} /> : null}
                <span className="rf-nav__txt">{n.label}</span>
                <Icon name="chevron_right" className={`rf-nav__chev ${open ? 'rf-nav__chev--open' : ''}`} />
              </button>
              {open ? (
                <div className="rf-nav__sub">
                  <NavItems nodes={n.children} depth={depth + 1} selectedKey={selectedKey} openKeys={openKeys} toggle={toggle} go={go} />
                </div>
              ) : null}
            </div>
          );
        }
        return (
          <button
            key={n.key}
            type="button"
            className={`rf-nav__item ${selectedKey === n.key ? 'rf-nav__item--active' : ''}`}
            onClick={() => n.to && go(n.to)}
          >
            {n.icon ? <Icon name={n.icon} size={19} /> : null}
            <span className="rf-nav__txt">{n.label}</span>
          </button>
        );
      })}
    </>
  );
}

export function AppShell() {
  const { has, email, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const unread = useUnreadCount();
  const [collapsed, setCollapsed] = useState(false);
  const [q, setQ] = useState('');

  const menu = useMemo(() => filterByPerm(MENU, has), [has]);

  const selected = findSelected(location.pathname, location.pathname + location.search);
  const initialOpen = selected ? ancestorKeys(MENU, selected.key) ?? [] : ['g-workspace'];
  const [openKeys, setOpenKeys] = useState<string[]>(initialOpen);
  const toggle = (k: string) => setOpenKeys((ks) => (ks.includes(k) ? ks.filter((x) => x !== k) : [...ks, k]));

  return (
    <div className="rf-shell">
      <aside className={`rf-rail ${collapsed ? 'rf-rail--off' : ''}`}>
        <div className="rf-brand">
          <span className="rf-brand__logo">T</span>
          <span className="rf-brand__name">TourKit</span>
        </div>
        <nav className="rf-nav">
          <div className="rf-nav__label">Điều hành</div>
          <NavItems nodes={menu} depth={0} selectedKey={selected?.key} openKeys={openKeys} toggle={toggle} go={(to) => navigate(to)} />
        </nav>
      </aside>

      <div className="rf-main">
        <header className="rf-topbar">
          <IconButton icon={collapsed ? 'menu' : 'menu_open'} onClick={() => setCollapsed((v) => !v)} title={collapsed ? 'Mở rộng menu' : 'Thu gọn menu'} />
          <SearchInput value={q} onChange={setQ} placeholder="Tìm kiếm khách hàng, đơn hàng..." filled style={{ maxWidth: 340, flex: 1 }} />
          <div style={{ flex: 1 }} />
          <Dropdown
            trigger={
              <Button variant="primary" icon="add">
                Tạo nhanh
              </Button>
            }
            items={[
              { key: 'order', label: 'Tạo đơn hàng', icon: 'shopping_cart', onClick: () => navigate('/orders') },
              { key: 'customer', label: 'Thêm khách hàng', icon: 'person_add', onClick: () => navigate('/customers') },
              { key: 'receipt', label: 'Lập phiếu thu', icon: 'receipt_long', onClick: () => navigate('/receipts') },
              { key: 'task', label: 'Tạo công việc', icon: 'task_alt', onClick: () => navigate('/work-tasks') },
            ]}
          />
          <Tooltip title="Thông báo">
            <Badge count={unread.data ?? 0}>
              <IconButton icon="notifications" onClick={() => navigate('/notifications')} title="Thông báo" />
            </Badge>
          </Tooltip>
          <Dropdown
            trigger={
              <span className="rf-user">
                <span style={{ textAlign: 'right', lineHeight: 1.25 }}>
                  <span className="rf-user__name" style={{ display: 'block' }}>
                    {email}
                  </span>
                  <span className="rf-user__role">Nhân viên</span>
                </span>
                <Avatar size={32}>
                  <Icon name="person" size={18} />
                </Avatar>
              </span>
            }
            items={[{ key: 'logout', label: 'Đăng xuất', icon: 'logout', danger: true, onClick: logout }]}
          />
        </header>

        <main className="rf-content">
          {/* Giới hạn bề rộng + căn giữa: tránh nội dung giãn thưa trên màn siêu rộng. */}
          <div className="rf-content__in">
            <AutoBreadcrumb pathname={location.pathname} fullPath={location.pathname + location.search} />
            <ErrorBoundary key={location.pathname}>
              <Outlet />
            </ErrorBoundary>
          </div>
        </main>
      </div>
    </div>
  );
}
