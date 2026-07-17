import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MessageProvider } from '../../ui/message';
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
      <MessageProvider>
        <CustomersPage />
      </MessageProvider>
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

  // Hệ Refined hiện số đếm trong chip riêng cạnh nhãn (thay nhãn gộp "Tiềm năng (30)").
  it('hiện chip phễu (segment + count) và chăm sóc', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Phễu khách hàng')).toBeInTheDocument();
      const seg = screen.getByRole('button', { name: /Tiềm năng/ });
      expect(seg).toHaveTextContent('30');
      // "Tất cả" xuất hiện cả ở tab loại KH -> lấy đúng chip phễu (chip mang count 42).
      const all = screen.getAllByRole('button', { name: /Tất cả/ }).find((b) => b.textContent?.includes('42'));
      expect(all).toBeDefined();
      const nc7 = screen.getByRole('button', { name: /7 ngày chưa liên hệ/ });
      expect(nc7).toHaveTextContent('1');
    });
  });

  it('Tag và NV phụ trách là Select (dropdown), không phải ô text tự gõ', async () => {
    const { container } = renderPage();
    await waitFor(() => expect(screen.getByText('Xem thêm bộ lọc')).toBeInTheDocument());
    fireEvent.click(screen.getByText('Xem thêm bộ lọc'));
    await waitFor(() => {
      // span đầu = nhãn placeholder (span sau là icon Material Symbols "expand_more").
      const placeholders = [...container.querySelectorAll('.rf-select__btn--placeholder > span:first-child')].map((e) => e.textContent);
      expect(placeholders).toContain('Tag');
      expect(placeholders).toContain('NV phụ trách');
    });
  });
});
