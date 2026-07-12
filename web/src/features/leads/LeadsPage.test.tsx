import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { App } from 'antd';
import { LeadsPage } from './LeadsPage';
import { httpClient } from '../../shared/api/httpClient';

vi.mock('../../shared/api/httpClient', () => ({ httpClient: { get: vi.fn(), post: vi.fn(), put: vi.fn() } }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ has: () => true }) }));
vi.mock('./leadsCrud', () => ({
  leadsCrud: {
    useCreate: () => ({ mutateAsync: vi.fn(), isPending: false }),
    useUpdate: () => ({ mutateAsync: vi.fn(), isPending: false }),
    useRemove: () => ({ mutateAsync: vi.fn(), isPending: false }),
  },
}));

const stats = { total: 6, new: 2, contacted: 1, qualified: 1, won: 1, lost: 1, converted: 1 };
const list = { items: [], total: 0, page: 1, size: 20 };

function mockGet(url: string) {
  if (url.includes('/leads/stats')) return Promise.resolve({ data: stats });
  if (url.includes('/leads/filter-options')) return Promise.resolve({ data: { sources: [] } });
  if (url.includes('/branches')) return Promise.resolve({ data: [] });
  if (url.includes('/users')) return Promise.resolve({ data: [] });
  return Promise.resolve({ data: list });
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <App>
        <LeadsPage />
      </App>
    </QueryClientProvider>,
  );
}

describe('LeadsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (httpClient.get as ReturnType<typeof vi.fn>).mockImplementation(mockGet);
  });

  it('gọi stats + facets + users + board khi mount', async () => {
    renderPage();
    await waitFor(() => {
      const urls = (httpClient.get as ReturnType<typeof vi.fn>).mock.calls.map((c) => c[0] as string);
      expect(urls).toContain('/api/v1/leads/stats');
      expect(urls).toContain('/api/v1/leads/filter-options');
      expect(urls).toContain('/api/v1/users');
      expect(urls).toContain('/api/v1/leads');
    });
  });

  it('hiện thẻ pipeline + toggle Kanban/List', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Tổng cơ hội')).toBeInTheDocument();
      expect(screen.getByText('Đã chuyển KH')).toBeInTheDocument();
      expect(screen.getByText('Kanban')).toBeInTheDocument(); // view toggle
      expect(screen.getByText('List')).toBeInTheDocument();
    });
  });
});
