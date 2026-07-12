import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { App } from 'antd';
import { ProvidersPage } from './ProvidersPage';
import { httpClient } from '../../shared/api/httpClient';

vi.mock('../../shared/api/httpClient', () => ({ httpClient: { get: vi.fn() } }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ has: () => true }) }));
vi.mock('./providersCrud', () => ({
  providersCrud: {
    useCreate: () => ({ mutateAsync: vi.fn(), isPending: false }),
    useUpdate: () => ({ mutateAsync: vi.fn(), isPending: false }),
    useRemove: () => ({ mutateAsync: vi.fn(), isPending: false }),
  },
}));

const stats = { total: 5, active: 4, inactive: 1 };
const list = { items: [], total: 0, page: 1, size: 20 };

function mockGet(url: string) {
  if (url.includes('/providers/stats')) return Promise.resolve({ data: stats });
  if (url.includes('/payment-terms')) return Promise.resolve({ data: [] });
  if (url.includes('/branches')) return Promise.resolve({ data: [] });
  if (url.includes('/market-types')) return Promise.resolve({ data: [] });
  return Promise.resolve({ data: list });
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <App>
        <ProvidersPage />
      </App>
    </QueryClientProvider>,
  );
}

describe('ProvidersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (httpClient.get as ReturnType<typeof vi.fn>).mockImplementation(mockGet);
  });

  it('gọi stats + list khi mount', async () => {
    renderPage();
    await waitFor(() => {
      const urls = (httpClient.get as ReturnType<typeof vi.fn>).mock.calls.map((c) => c[0] as string);
      expect(urls).toContain('/api/v1/providers/stats');
      expect(urls).toContain('/api/v1/providers');
    });
  });

  it('hiện thẻ thống kê + tabs loại NCC', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Tổng số NCC')).toBeInTheDocument();
      expect(screen.getByText('Đang hoạt động')).toBeInTheDocument();
      expect(screen.getByText('Khách sạn')).toBeInTheDocument(); // tab loại
    });
  });
});
