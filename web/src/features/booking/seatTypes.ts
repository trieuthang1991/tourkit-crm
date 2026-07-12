import { z } from 'zod';

export const orderSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  tourDepartureId: z.string().uuid(),
  customerId: z.string().uuid(),
  totalRevenue: z.number(),
  totalCost: z.number(),
  status: z.number(),
  salesUserId: z.string().uuid().nullable(),
  customerName: z.string().nullable().optional(),
  tourTitle: z.string().nullable().optional(),
  departureDate: z.string().nullable().optional(),
  amountPaid: z.number().optional(),
  outstanding: z.number().optional(),
});
export type Order = z.infer<typeof orderSchema>;

export const seatSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  status: z.number(),
  upfrontAmount: z.number(),
  lineTotal: z.number(),
  holdExpiresAt: z.string().nullable(),
  reservationCode: z.string().nullable(),
});
export type Seat = z.infer<typeof seatSchema>;

export type BookingRequestForm = {
  customerId: string;
  adultQty: number;
  childQty: number;
  childSmallQty: number;
  babyQty: number;
};

export const SEAT_STATUS: Record<number, string> = {
  1: 'Giữ chỗ',
  2: 'Đã xác nhận',
  3: 'Đặt cọc',
  4: 'Đã thanh toán',
  5: 'Đã huỷ',
};

export const ORDER_STATUS: Record<number, string> = {
  1: 'Nháp',
  2: 'Chốt',
  3: 'Huỷ',
};
