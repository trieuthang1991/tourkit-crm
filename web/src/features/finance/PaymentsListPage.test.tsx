import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { App } from 'antd';
import { PaymentsListPage } from './PaymentsListPage';
import { httpClient } from '../../shared/api/httpClient';

vi.mock('../../shared/api/httpClient', () => ({ httpClient: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ has: () => true }) }));

const stats = { total: 3, totalAmount: 6000000, pending: 1, approved: 1, rejected: 1 };
const list = { items: [], total: 0, page: 1, size: 20 };

function mockGet(url: string) {
  if (url.includes('/payments/stats')) return Promise.resolve({ data: stats });
  if (url.includes('/branches')) return Promise.resolve({ data: [] });
  return Promise.resolve({ data: list });
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <App>
        <PaymentsListPage />
      </App>
    </QueryClientProvider>,
  );
}

describe('PaymentsListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (httpClient.get as ReturnType<typeof vi.fn>).mockImplementation(mockGet);
  });

  it('gọi stats + list', async () => {
    renderPage();
    await waitFor(() => {
      const urls = (httpClient.get as ReturnType<typeof vi.fn>).mock.calls.map((c) => c[0] as string);
      expect(urls).toContain('/api/v1/payments/stats');
      expect(urls).toContain('/api/v1/payments');
    });
  });

  it('hiện thẻ thống kê + tabs trạng thái', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Tổng số phiếu')).toBeInTheDocument();
      expect(screen.getByText('Tất cả')).toBeInTheDocument();
    });
  });
});
