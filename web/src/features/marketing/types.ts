import { z } from 'zod';

export const campaignSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  channel: z.number(),
  subject: z.string().nullable(),
  body: z.string(),
  status: z.number(),
});
export type Campaign = z.infer<typeof campaignSchema>;

const campaignBaseFields = {
  name: z.string().min(1, 'Bắt buộc'),
  channel: z.number(),
  subject: z.string().nullable().transform((v) => (v ? v : null)),
  body: z.string().min(1, 'Bắt buộc'),
};

export const campaignCreateSchema = z.object(campaignBaseFields);
export type CampaignCreateForm = z.infer<typeof campaignCreateSchema>;

export const campaignUpdateSchema = z.object({
  ...campaignBaseFields,
  status: z.number(),
});
export type CampaignUpdateForm = z.infer<typeof campaignUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — status is optional so both
// campaignCreateSchema (no status) and campaignUpdateSchema (status required) outputs satisfy it.
export type CampaignForm = CampaignCreateForm & { status?: number };

export const CHANNEL: Record<number, string> = {
  1: 'Email',
  2: 'SMS',
  3: 'Zalo',
};

export const campaignLogSchema = z.object({
  id: z.string().uuid(),
  recipient: z.string(),
  status: z.number(),
  sentAt: z.string(),
});
export type CampaignLog = z.infer<typeof campaignLogSchema>;
