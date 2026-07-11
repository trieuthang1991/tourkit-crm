import { z } from 'zod';

export const SERVICE_BOOKING_TYPE: Record<number, string> = {
  1: 'Khách sạn',
  2: 'Vé máy bay',
  3: 'Visa',
  4: 'Vé',
  5: 'Đưa đón',
  99: 'Khác',
};

export const serviceBookingSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  type: z.number(),
  orderId: z.string().uuid().nullable(),
  providerId: z.string().uuid().nullable(),
  description: z.string(),
  startDate: z.string().nullable(),
  endDate: z.string().nullable(),
  quantity: z.number(),
  unitPrice: z.number(),
  totalAmount: z.number(),
  status: z.number(),
  note: z.string().nullable(),
  roomClassId: z.string().uuid().nullable(),
});
export type ServiceBooking = z.infer<typeof serviceBookingSchema>;

export const serviceBookingFormSchema = z.object({
  code: z.string(),
  type: z.number(),
  orderId: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  providerId: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  description: z.string().min(1, 'Bắt buộc'),
  startDate: z.string().nullable(),
  endDate: z.string().nullable(),
  quantity: z.number(),
  unitPrice: z.number(),
  status: z.number(),
  note: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  roomClassId: z.string().nullable(),
});
export type ServiceBookingForm = z.infer<typeof serviceBookingFormSchema>;
