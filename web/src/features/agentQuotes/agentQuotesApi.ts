import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { agentQuoteSchema } from './types';
import type { CreateAgentQuoteForm, QuoteActionForm } from './types';

const KEY = ['agentQuotes'];

export function useAgentQuotes(page: number, size: number) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agent-quotes', { params: { page, size } });
      return pagedSchema(agentQuoteSchema).parse(data);
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
