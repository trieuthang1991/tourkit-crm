import { describe, expect, it, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { z } from 'zod';
import type { ReactNode } from 'react';
import { makeCrud } from './useCrudResource';
import { httpClient } from '../api/httpClient';

vi.mock('../api/httpClient', () => ({ httpClient: { get: vi.fn() } }));

const crud = makeCrud({
  key: 'widgets',
  basePath: '/api/v1/widgets',
  itemSchema: z.object({ id: z.string(), name: z.string() }),
  getId: (w) => w.id,
});

function wrapper({ children }: { children: ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
}

describe('makeCrud.useList', () => {
  beforeEach(() => vi.clearAllMocks());
  it('parses a paged response', async () => {
    (httpClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { items: [{ id: '1', name: 'A' }], total: 1, page: 1, size: 20 },
    });
    const { result } = renderHook(() => crud.useList({ page: 1, size: 20 }), { wrapper });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.items[0]?.name).toBe('A');
  });
});
