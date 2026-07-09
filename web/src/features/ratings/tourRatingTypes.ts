import { z } from 'zod';

export const tourRatingSchema = z.object({
  id: z.string().uuid(),
  tourDepartureId: z.string().uuid().nullable(),
  orderId: z.string().uuid().nullable(),
  customerName: z.string().nullable(),
  customerPhone: z.string().nullable(),
  stars: z.number(),
  comment: z.string().nullable(),
  status: z.number(),
});
export type TourRating = z.infer<typeof tourRatingSchema>;

const tourRatingCommonFields = {
  customerName: z.string().nullable().transform((v) => (v ? v : null)),
  customerPhone: z.string().nullable().transform((v) => (v ? v : null)),
  stars: z.number().min(1, 'Từ 1 đến 5').max(5, 'Từ 1 đến 5'),
  comment: z.string().nullable().transform((v) => (v ? v : null)),
  status: z.number(),
};

export const tourRatingCreateSchema = z.object({
  tourDepartureId: z.string().nullable().transform((v) => (v ? v : null)),
  orderId: z.string().nullable().transform((v) => (v ? v : null)),
  ...tourRatingCommonFields,
});
export type TourRatingCreateForm = z.infer<typeof tourRatingCreateSchema>;

export const tourRatingUpdateSchema = z.object(tourRatingCommonFields);
export type TourRatingUpdateForm = z.infer<typeof tourRatingUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — tourDepartureId/orderId are optional
// (create only) so both tourRatingCreateSchema and tourRatingUpdateSchema outputs satisfy it.
export type TourRatingForm = TourRatingUpdateForm & {
  tourDepartureId?: string | null;
  orderId?: string | null;
};
