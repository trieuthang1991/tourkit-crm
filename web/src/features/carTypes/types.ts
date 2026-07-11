import { z } from 'zod';

export const carTypeSchema = z.object({
  id: z.string().uuid(),
  code: z.number(),
  name: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type CarType = z.infer<typeof carTypeSchema>;

export const carTypeCreateSchema = z.object({
  code: z.number().int().min(1, 'Số ghế > 0'),
  name: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
export type CarTypeCreateForm = z.infer<typeof carTypeCreateSchema>;
