import { z } from 'zod';

export const marketTypeSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  parentId: z.string().uuid().nullable(),
  sortOrder: z.number(),
  status: z.number(),
});
export type MarketType = z.infer<typeof marketTypeSchema>;

export const marketTypeCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  parentId: z.string().nullable().transform((v) => (v ? v : null)),
  sortOrder: z.number(),
});
export type MarketTypeCreateForm = z.infer<typeof marketTypeCreateSchema>;
