import { z } from 'zod';

export const customerSourceSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type CustomerSource = z.infer<typeof customerSourceSchema>;

export const customerSourceCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
export type CustomerSourceCreateForm = z.infer<typeof customerSourceCreateSchema>;
