import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { campaignSchema } from './types';

const KEY = ['campaigns'];

export type CampaignFilter = { q?: string; channel?: number; status?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useCampaigns(page: number, size: number, filter: CampaignFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/marketing/campaigns', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(campaignSchema).parse(data);
    },
  });
}

export const campaignStatsSchema = z.object({
  total: z.number(),
  draft: z.number(),
  sent: z.number(),
  messages: z.number(),
});

export function useCampaignStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/marketing/campaigns/stats');
      return campaignStatsSchema.parse(data);
    },
  });
}
