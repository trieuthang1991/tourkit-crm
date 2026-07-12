import { z } from 'zod';

export const departureSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  title: z.string(),
  templateId: z.string().uuid().nullable(),
  departureDate: z.string().nullable(),
  endDate: z.string().nullable(),
  totalSlots: z.number(),
  status: z.number(),
  tourType: z.string().nullable().optional(),
  assignedToUserId: z.string().uuid().nullable().optional(),
  isClosed: z.boolean().optional(),
});
export type Departure = z.infer<typeof departureSchema>;

export const departureFormSchema = z.object({
  templateId: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  code: z.string().min(1, 'Bắt buộc'),
  title: z.string().min(1, 'Bắt buộc'),
  departureDate: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  endDate: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  totalSlots: z.number(),
});
export type DepartureForm = z.infer<typeof departureFormSchema>;
