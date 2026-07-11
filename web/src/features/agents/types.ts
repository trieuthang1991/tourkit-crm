import { z } from 'zod';

const nullableStr = z
  .string()
  .nullable()
  .transform((v) => (v ? v : null));

export const agentSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  name: z.string(),
  contactPerson: z.string().nullable(),
  phone: z.string().nullable(),
  email: z.string().nullable(),
  taxCode: z.string().nullable(),
  address: z.string().nullable(),
  creditLimit: z.number(),
  status: z.number(),
});
export type Agent = z.infer<typeof agentSchema>;

export const agentFormSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  name: z.string().min(1, 'Bắt buộc'),
  contactPerson: nullableStr,
  phone: nullableStr,
  email: nullableStr,
  taxCode: nullableStr,
  address: nullableStr,
  creditLimit: z.number(),
  status: z.number(),
});
export type AgentForm = z.infer<typeof agentFormSchema>;
