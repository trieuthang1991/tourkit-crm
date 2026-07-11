import { z } from 'zod';

export const departmentSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  code: z.string().nullable(),
  sortOrder: z.number(),
  status: z.number(),
});
export type Department = z.infer<typeof departmentSchema>;

export const departmentCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  code: z.string().nullable().transform((v) => (v ? v : null)),
  sortOrder: z.number(),
});
export type DepartmentCreateForm = z.infer<typeof departmentCreateSchema>;

export const positionSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type Position = z.infer<typeof positionSchema>;

export const positionCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
export type PositionCreateForm = z.infer<typeof positionCreateSchema>;
