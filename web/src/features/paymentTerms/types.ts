import { z } from 'zod';

export const paymentTermSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  description: z.string().nullable(),
  sortOrder: z.number(),
  status: z.number(),
});
export type PaymentTerm = z.infer<typeof paymentTermSchema>;

export const paymentTermCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  description: z.string().nullable().transform((v) => (v ? v : null)),
  sortOrder: z.number(),
});
export type PaymentTermCreateForm = z.infer<typeof paymentTermCreateSchema>;
