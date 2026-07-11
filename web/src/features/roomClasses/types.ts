import { z } from 'zod';

export const roomClassSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type RoomClass = z.infer<typeof roomClassSchema>;

export const roomClassCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
export type RoomClassCreateForm = z.infer<typeof roomClassCreateSchema>;
