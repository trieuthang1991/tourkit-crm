import { z } from 'zod';

const nullableText = z
  .string()
  .nullable()
  .transform((v) => (v ? v : null));

export const customerSchema = z.object({
  id: z.string().uuid(),
  fullName: z.string(),
  phone: z.string().nullable(),
  customerType: z.number(),
  source: z.string().nullable(),
  tag: z.string().nullable(),
  tempBalance: z.number(),
  email: z.string().nullable(),
  address: z.string().nullable(),
  dateOfBirth: z.string().nullable(),
  idCardNumber: z.string().nullable(),
  passportNumber: z.string().nullable(),
  passportExpiry: z.string().nullable(),
  nationality: z.string().nullable(),
});
export type Customer = z.infer<typeof customerSchema>;

export const customerFormSchema = z.object({
  fullName: z.string().min(1, 'Bắt buộc'),
  phone: nullableText,
  customerType: z.number(),
  source: nullableText,
  tag: nullableText,
  tempBalance: z.number(),
  email: nullableText,
  address: nullableText,
  dateOfBirth: z.string().nullable(),
  idCardNumber: nullableText,
  passportNumber: nullableText,
  passportExpiry: z.string().nullable(),
  nationality: nullableText,
});
export type CustomerForm = z.infer<typeof customerFormSchema>;
