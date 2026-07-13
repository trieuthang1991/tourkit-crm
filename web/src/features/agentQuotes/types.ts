import { z } from 'zod';

export const AGENT_QUOTE_STATUS: Record<number, string> = {
  1: 'Chờ báo giá',
  2: 'Đã chào giá',
  3: 'Đã xác nhận',
  4: 'Từ chối',
};

export const agentQuoteSchema = z.object({
  id: z.string().uuid(),
  agentId: z.string().uuid(),
  productName: z.string(),
  travelDate: z.string().nullable(),
  returnDate: z.string().nullable(),
  paxCount: z.number(),
  specialRequests: z.string().nullable(),
  status: z.number(),
  quotedAmount: z.number().nullable(),
  quotedNote: z.string().nullable(),
  agentName: z.string().nullable().optional(),
});
export type AgentQuote = z.infer<typeof agentQuoteSchema>;

export const createAgentQuoteFormSchema = z.object({
  agentId: z.string().min(1, 'Bắt buộc'),
  productName: z.string().min(1, 'Bắt buộc'),
  travelDate: z.string().nullable(),
  returnDate: z.string().nullable(),
  paxCount: z.number(),
  specialRequests: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
});
export type CreateAgentQuoteForm = z.infer<typeof createAgentQuoteFormSchema>;

export const quoteActionFormSchema = z.object({
  quotedAmount: z.number(),
  quotedNote: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
});
export type QuoteActionForm = z.infer<typeof quoteActionFormSchema>;
