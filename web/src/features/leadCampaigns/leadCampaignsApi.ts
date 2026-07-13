import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';

const KEY = ['lead-campaigns'];

export const leadCampaignSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  createdByUserId: z.string().uuid().nullable(),
  createdByName: z.string().nullable(),
  createdAt: z.string(),
  status: z.number(),
  totalLeads: z.number(),
  caredCount: z.number(),
  closedCount: z.number(),
  progress: z.number(),
  closeRate: z.number(),
});
export type LeadCampaign = z.infer<typeof leadCampaignSchema>;

export const leadCampaignStatsSchema = z.object({
  totalCampaigns: z.number(),
  totalLeads: z.number(),
  avgCloseRate: z.number(),
  completed: z.number(),
});

/// Trạng thái chiến dịch: 0 đang chạy, 1 hoàn thành.
export const LEAD_CAMPAIGN_STATUS: Record<number, string> = { 0: 'Đang chạy', 1: 'Hoàn thành' };

export type LeadCampaignFilter = { q?: string };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useLeadCampaigns(page: number, size: number, filter: LeadCampaignFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/lead-campaigns', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(leadCampaignSchema).parse(data);
    },
  });
}

export function useLeadCampaignStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/lead-campaigns/stats');
      return leadCampaignStatsSchema.parse(data);
    },
  });
}

export function useCreateLeadCampaign() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; note: string | null }) => {
      await httpClient.post('/api/v1/lead-campaigns', body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
