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
