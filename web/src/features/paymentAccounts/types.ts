import { z } from 'zod';

export const paymentAccountSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  bankName: z.string().nullable(),
  accountNumber: z.string().nullable(),
  accountHolder: z.string().nullable(),
  branch: z.string().nullable(),
  transferNote: z.string().nullable(),
  isDefault: z.boolean(),
  sortOrder: z.number(),
  status: z.number(),
});
export type PaymentAccount = z.infer<typeof paymentAccountSchema>;

const nullableText = z
  .string()
  .nullable()
  .transform((v) => (v ? v : null));

export const paymentAccountCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  bankName: nullableText,
  accountNumber: nullableText,
  accountHolder: nullableText,
  branch: nullableText,
  transferNote: nullableText,
  isDefault: z.boolean(),
  sortOrder: z.number(),
});
export type PaymentAccountCreateForm = z.infer<typeof paymentAccountCreateSchema>;
