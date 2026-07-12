import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { App } from 'antd';
import { OrdersPage } from './OrdersPage';
import { httpClient } from '../../shared/api/httpClient';

vi.mock('../../shared/api/httpClient', () => ({ httpClient: { get: vi.fn() } }));

const stats = {
  total: 3, totalRevenue: 13000000, totalPaid: 5000000, totalOutstanding: 8000000,
  draft: 1, confirmed: 1, cancelled: 1, unpaid: 1, deposit: 1, paid: 1,
};
const list = { items: [], total: 0, page: 1, size: 20 };

function mockGet(url: string) {
  if (url.includes('/orders/stats')) return Promise.resolve({ data: stats });
  if (url.includes('/orders/filter-options')) return Promise.resolve({ data: { tourTypes: [], providers: [] } });
  if (url.includes('/market-types')) return Promise.resolve({ data: [] });
  if (url.includes('/tour-groups')) return Promise.resolve({ data: [] });
  if (url.includes('/branches')) return Promise.resolve({ data: [] });
  if (url.includes('/departments')) return Promise.resolve({ data: [] });
  if (url.includes('/users')) return Promise.resolve({ data: [] });
  return Promise.resolve({ data: list });
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <App>
        <MemoryRouter>
          <OrdersPage />
        </MemoryRouter>
      </App>
    </QueryClientProvider>,
  );
}

describe('OrdersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (httpClient.get as ReturnType<typeof vi.fn>).mockImplementation(mockGet);
  });

  it('gọi stats + list khi mount', async () => {
    renderPage();
    await waitFor(() => {
      const urls = (httpClient.get as ReturnType<typeof vi.fn>).mock.calls.map((c) => c[0] as string);
      expect(urls).toContain('/api/v1/orders/stats');
      expect(urls).toContain('/api/v1/orders');
    });
  });

  it('hiện thẻ thống kê + tabs trạng thái', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Tổng số đơn')).toBeInTheDocument();
      expect(screen.getByText('Đã chốt')).toBeInTheDocument();
      expect(screen.getByText('Chưa thanh toán (1)')).toBeInTheDocument(); // tab trạng thái thanh toán
    });
  });
});
