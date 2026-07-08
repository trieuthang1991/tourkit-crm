import { z } from 'zod';

export const tourTemplateSchema = z.object({
  id: z.string(),
  code: z.string(),
  title: z.string(),
  tourType: z.string().nullable(),
  totalSlots: z.number(),
  reservationHours: z.number(),
  priceAdult: z.number(),
  priceChild: z.number(),
  priceChildSmall: z.number(),
  priceBaby: z.number(),
  termsNote: z.string().nullable(),
  status: z.number(),
});

export type TourTemplate = z.infer<typeof tourTemplateSchema>;

const tourTemplateCommonFields = {
  title: z.string().min(1, 'Bắt buộc'),
  tourType: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  totalSlots: z.number(),
  reservationHours: z.number(),
  priceAdult: z.number(),
  priceChild: z.number(),
  priceChildSmall: z.number(),
  priceBaby: z.number(),
  termsNote: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
};

export const tourTemplateCreateSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  ...tourTemplateCommonFields,
});
export type TourTemplateCreateForm = z.infer<typeof tourTemplateCreateSchema>;

export const tourTemplateUpdateSchema = z.object(tourTemplateCommonFields);
export type TourTemplateUpdateForm = z.infer<typeof tourTemplateUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — code is optional so both
// tourTemplateCreateSchema (with code) and tourTemplateUpdateSchema (no code) outputs satisfy it.
export type TourTemplateForm = TourTemplateUpdateForm & { code?: string };
