import { z } from 'zod';

export const messageTemplateSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  channel: z.number(),
  subject: z.string().nullable(),
  body: z.string(),
});
export type MessageTemplate = z.infer<typeof messageTemplateSchema>;

export const messageTemplateFormSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  channel: z.number(),
  subject: z.string().nullable().transform((v) => (v ? v : null)),
  body: z.string().min(1, 'Bắt buộc'),
});
export type MessageTemplateForm = z.infer<typeof messageTemplateFormSchema>;

export const TEMPLATE_CHANNEL: Record<number, string> = {
  1: 'Email',
  2: 'SMS',
  3: 'Zalo',
};
