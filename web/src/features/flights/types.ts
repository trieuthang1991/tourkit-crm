import { z } from 'zod';

export const flightSegmentSchema = z.object({
  date: z.string().nullable().optional(),
  flightNo: z.string().nullable().optional(),
  from: z.string().nullable().optional(),
  to: z.string().nullable().optional(),
  depTime: z.string().nullable().optional(),
});
export type FlightSegment = z.infer<typeof flightSegmentSchema>;

export const flightTicketSchema = z.object({
  id: z.string().uuid(),
  pnr: z.string(),
  marketRef: z.string().nullable(),
  marketName: z.string().nullable(),
  providerRef: z.string().nullable(),
  providerName: z.string().nullable(),
  tourType: z.string().nullable(),
  days: z.number(),
  departureDate: z.string().nullable(),
  quantity: z.number(),
  usedQuantity: z.number(),
  remainingQuantity: z.number(),
  orderRef: z.string().nullable(),
  orderCode: z.string().nullable(),
  orderName: z.string().nullable(),
  totalCost: z.number(),
  paidAmount: z.number(),
  remainingCost: z.number(),
  reservedAmount: z.number(),
  status: z.number(),
  note: z.string().nullable(),
  segments: z.array(flightSegmentSchema),
});
export type FlightTicket = z.infer<typeof flightTicketSchema>;

export const flightTicketStatsSchema = z.object({
  total: z.number(),
  assigned: z.number(),
  unassigned: z.number(),
  totalQuantity: z.number(),
  totalUsed: z.number(),
  totalRemaining: z.number(),
  totalCost: z.number(),
  totalPaid: z.number(),
  totalRemainingCost: z.number(),
  totalReserved: z.number(),
});
export type FlightTicketStats = z.infer<typeof flightTicketStatsSchema>;

/// Loại hình (bám Order/Departure TourType).
export const FLIGHT_TOUR_TYPE: Record<string, string> = {
  inbound: 'Inbound',
  outbound: 'Outbound',
  domestic: 'Nội địa',
};

export const createFlightTicketFormSchema = z.object({
  pnr: z.string().min(1, 'Bắt buộc'),
  marketRef: z.string().nullable(),
  providerRef: z.string().nullable(),
  tourType: z.string().nullable(),
  days: z.number(),
  departureDate: z.string().nullable(),
  quantity: z.number(),
  totalCost: z.number(),
  reservedAmount: z.number(),
  note: z.string().nullable(),
});
export type CreateFlightTicketForm = z.infer<typeof createFlightTicketFormSchema>;
