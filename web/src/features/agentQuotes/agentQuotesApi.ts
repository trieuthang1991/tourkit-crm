import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { agentQuoteSchema } from './types';
import type { CreateAgentQuoteForm, QuoteActionForm } from './types';

const KEY = ['agentQuotes'];

export type AgentQuoteFilter = { q?: string; agentId?: string; status?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useAgentQuotes(page: number, size: number, filter: AgentQuoteFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agent-quotes', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(agentQuoteSchema).parse(data);
    },
  });
}

export const agentQuoteStatsSchema = z.object({
  total: z.number(),
  requested: z.number(),
  quoted: z.number(),
  confirmed: z.number(),
  rejected: z.number(),
  totalQuoted: z.number(),
});

export function useAgentQuoteStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agent-quotes/stats');
      return agentQuoteStatsSchema.parse(data);
    },
  });
}

export function useCreateAgentQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateAgentQuoteForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/agent-quotes', body);
      return agentQuoteSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useQuoteAgentRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: QuoteActionForm }) => {
      await httpClient.post(`/api/v1/agent-quotes/${id}/quote`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useConfirmAgentQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/agent-quotes/${id}/confirm`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useRejectAgentQuote() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/agent-quotes/${id}/reject`, { note: null });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
