import { z } from 'zod';

export const customerSchema = z.object({
  id: z.string().uuid(),
  fullName: z.string(),
  phone: z.string().nullable(),
  customerType: z.number(),
  source: z.string().nullable(),
  tag: z.string().nullable(),
  tempBalance: z.number(),
});
export type Customer = z.infer<typeof customerSchema>;

export const customerFormSchema = z.object({
  fullName: z.string().min(1, 'Bắt buộc'),
  phone: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  customerType: z.number(),
  source: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  tag: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  tempBalance: z.number(),
});
export type CustomerForm = z.infer<typeof customerFormSchema>;
