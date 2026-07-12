import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { App } from 'antd';
import { ReceiptsListPage } from './ReceiptsListPage';
import { httpClient } from '../../shared/api/httpClient';

vi.mock('../../shared/api/httpClient', () => ({ httpClient: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ has: () => true }) }));

const stats = { total: 4, totalAmount: 9000000, pending: 1, approved: 2, rejected: 1 };
const list = { items: [], total: 0, page: 1, size: 20 };

function mockGet(url: string) {
  if (url.includes('/receipts/stats')) return Promise.resolve({ data: stats });
  if (url.includes('/branches')) return Promise.resolve({ data: [] });
  if (url.includes('/users')) return Promise.resolve({ data: [] });
  return Promise.resolve({ data: list });
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <App>
        <ReceiptsListPage />
      </App>
    </QueryClientProvider>,
  );
}

describe('ReceiptsListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (httpClient.get as ReturnType<typeof vi.fn>).mockImplementation(mockGet);
  });

  it('gọi stats + list', async () => {
    renderPage();
    await waitFor(() => {
      const urls = (httpClient.get as ReturnType<typeof vi.fn>).mock.calls.map((c) => c[0] as string);
      expect(urls).toContain('/api/v1/receipts/stats');
      expect(urls).toContain('/api/v1/receipts');
    });
  });

  it('hiện thẻ thống kê + tabs trạng thái', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Tổng số phiếu')).toBeInTheDocument();
      expect(screen.getByText('Tổng tiền')).toBeInTheDocument();
      expect(screen.getByText('Tất cả')).toBeInTheDocument(); // tab
    });
  });
});
