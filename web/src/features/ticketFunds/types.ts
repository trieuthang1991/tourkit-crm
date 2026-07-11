import { z } from 'zod';

const nullableStr = z
  .string()
  .nullable()
  .transform((v) => (v ? v : null));

export const ticketFundSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  providerId: z.string().uuid().nullable(),
  providerServiceId: z.string().uuid().nullable(),
  ticketCode: z.string(),
  status: z.number(),
  isClosed: z.boolean(),
});
export type TicketFund = z.infer<typeof ticketFundSchema>;

export const ticketFundFormSchema = z.object({
  orderId: z.string().min(1, 'Bắt buộc'),
  providerId: nullableStr,
  providerServiceId: nullableStr,
  ticketCode: nullableStr,
  status: z.number(),
  isClosed: z.boolean(),
});
export type TicketFundForm = z.infer<typeof ticketFundFormSchema>;
