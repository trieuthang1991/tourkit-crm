import { z } from 'zod';

export const languageTypeSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  code: z.string().nullable(),
  sortOrder: z.number(),
  status: z.number(),
});
export type LanguageType = z.infer<typeof languageTypeSchema>;

export const languageTypeCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  code: z.string().nullable().transform((v) => (v ? v : null)),
  sortOrder: z.number(),
});
export type LanguageTypeCreateForm = z.infer<typeof languageTypeCreateSchema>;
