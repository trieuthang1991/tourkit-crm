import { z } from 'zod';

export const customerTagSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  color: z.string().nullable(),
  sortOrder: z.number(),
  status: z.number(),
});
export type CustomerTag = z.infer<typeof customerTagSchema>;

export const customerTagCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  color: z.string().nullable().transform((v) => (v ? v : null)),
  sortOrder: z.number(),
});
export type CustomerTagCreateForm = z.infer<typeof customerTagCreateSchema>;
