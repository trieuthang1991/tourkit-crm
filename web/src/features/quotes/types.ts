import { z } from 'zod';

export const quoteLineSchema = z.object({
  id: z.string(),
  description: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  amount: z.number(),
});

export const quoteSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  customerId: z.string().nullable(),
  customerName: z.string(),
  title: z.string(),
  validUntil: z.string().nullable(),
  status: z.number(),
  note: z.string().nullable(),
  totalAmount: z.number(),
  lines: z.array(quoteLineSchema),
});
export type Quote = z.infer<typeof quoteSchema>;

export const quoteSummarySchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  customerName: z.string(),
  title: z.string(),
  validUntil: z.string().nullable(),
  status: z.number(),
  totalAmount: z.number(),
});
export type QuoteSummary = z.infer<typeof quoteSummarySchema>;

export const quoteFormSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  customerName: z.string(),
  title: z.string().min(1, 'Bắt buộc'),
  validUntil: z.string().nullable(),
  status: z.number(),
  note: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  lines: z
    .array(
      z.object({
        description: z.string().min(1, 'Bắt buộc'),
        quantity: z.number(),
        unitPrice: z.number(),
      }),
    )
    .min(1, 'Cần ít nhất 1 dòng'),
});
export type QuoteForm = z.infer<typeof quoteFormSchema>;
