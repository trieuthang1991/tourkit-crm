import { z } from 'zod';

export const customerSchema = z.object({
  id: z.string().uuid(),
  fullName: z.string(),
  phone: z.string().nullable(),
});
export type Customer = z.infer<typeof customerSchema>;

export const customerFormSchema = z.object({
  fullName: z.string().min(1, 'Bắt buộc'),
  phone: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
});
export type CustomerForm = z.infer<typeof customerFormSchema>;
