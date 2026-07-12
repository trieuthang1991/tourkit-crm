import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { App } from 'antd';
import { cleanParams, CustomersPage } from './CustomersPage';
import { httpClient } from '../../shared/api/httpClient';

vi.mock('../../shared/api/httpClient', () => ({ httpClient: { get: vi.fn() } }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ has: () => true }) }));
vi.mock('./customersCrud', () => ({
  customersCrud: {
    useCreate: () => ({ mutateAsync: vi.fn(), isPending: false }),
    useUpdate: () => ({ mutateAsync: vi.fn(), isPending: false }),
    useRemove: () => ({ mutateAsync: vi.fn(), isPending: false }),
  },
}));

const facets = {
  sources: [], cities: [], marketGroups: [], campaigns: [], collaborators: [],
  branches: [], groups: [], departments: [], tags: ['VIP'], segments: [],
};
const stats = { total: 0, newToday: 0, newThisMonth: 0, firstTimeBuyers: 0, repeatBuyers: 0 };
const funnel = {
  total: 42,
  segments: [{ name: 'Tiềm năng', count: 30 }, { name: 'VIP', count: 5 }],
  care: { firstTime: 7, repeat: 3, notContacted7: 1, notContacted15: 2, notContacted30: 4, notContacted90: 9 },
};
const emptyList = { items: [], total: 0, page: 1, size: 20 };

function mockGet(url: string) {
  if (url.includes('/stats')) return Promise.resolve({ data: stats });
  if (url.includes('/filter-options')) return Promise.resolve({ data: facets });
  if (url.includes('/funnel')) return Promise.resolve({ data: funnel });
  if (url.includes('/users')) return Promise.resolve({ data: [{ id: crypto.randomUUID(), fullName: 'Admin Demo' }] });
  return Promise.resolve({ data: emptyList });
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <App>
        <CustomersPage />
      </App>
    </QueryClientProvider>,
  );
}

describe('cleanParams', () => {
  it('bỏ giá trị undefined / null / rỗng, giữ 0 và chuỗi hợp lệ', () => {
    expect(cleanParams({ a: 1, b: undefined, c: null, d: '', e: 'x', f: 0 })).toEqual({ a: 1, e: 'x', f: 0 });
  });
});

describe('CustomersPage — thanh lọc', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (httpClient.get as ReturnType<typeof vi.fn>).mockImplementation(mockGet);
  });

  it('gọi list + stats + facets + users khi mount', async () => {
    renderPage();
    await waitFor(() => {
      const urls = (httpClient.get as ReturnType<typeof vi.fn>).mock.calls.map((c) => c[0] as string);
      expect(urls).toContain('/api/v1/customers/stats');
      expect(urls).toContain('/api/v1/customers/filter-options');
      expect(urls).toContain('/api/v1/customers/funnel');
      expect(urls).toContain('/api/v1/users');
      expect(urls).toContain('/api/v1/customers');
    });
  });

  it('hiện chip phễu (segment + count) và chăm sóc', async () => {
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <App>
          <CustomersPage />
        </App>
      </QueryClientProvider>,
    );
    await waitFor(() => {
      expect(screen.getByText('Phễu khách hàng')).toBeInTheDocument();
      expect(screen.getByText('Tiềm năng (30)')).toBeInTheDocument();
      expect(screen.getByText('Tất cả (42)')).toBeInTheDocument();
      expect(screen.getByText('7 ngày chưa liên hệ (1)')).toBeInTheDocument();
    });
  });

  it('Tag và NV phụ trách là Select (dropdown), không phải ô text tự gõ', async () => {
    const { container } = renderPage();
    await waitFor(() => expect(screen.getByText('Xem thêm bộ lọc')).toBeInTheDocument());
    fireEvent.click(screen.getByText('Xem thêm bộ lọc'));
    await waitFor(() => {
      const placeholders = [...container.querySelectorAll('.ant-select-selection-placeholder')].map((e) => e.textContent);
      expect(placeholders).toContain('Tag');
      expect(placeholders).toContain('NV phụ trách');
    });
  });
});
