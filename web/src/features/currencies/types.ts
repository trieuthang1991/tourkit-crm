import { z } from 'zod';

export const currencySchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  name: z.string(),
  rateToVnd: z.number(),
  sortOrder: z.number(),
  status: z.number(),
});
export type Currency = z.infer<typeof currencySchema>;

export const currencyCreateSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  name: z.string().min(1, 'Bắt buộc'),
  rateToVnd: z.number().positive('Tỷ giá > 0'),
  sortOrder: z.number(),
});
export type CurrencyCreateForm = z.infer<typeof currencyCreateSchema>;
