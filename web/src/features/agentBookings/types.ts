import { z } from 'zod';

export const AGENT_BOOKING_STATUS: Record<number, string> = {
  0: 'Chờ',
  1: 'Xác nhận',
  2: 'Huỷ',
  3: 'Hoàn tất',
};

export const agentPassengerSchema = z.object({
  id: z.string().uuid(),
  fullName: z.string(),
  dateOfBirth: z.string().nullable(),
  passportNo: z.string().nullable(),
  nationality: z.string().nullable(),
  note: z.string().nullable(),
});
export type AgentPassenger = z.infer<typeof agentPassengerSchema>;

export const agentBookingSchema = z.object({
  id: z.string().uuid(),
  agentId: z.string().uuid(),
  quoteRequestId: z.string().uuid(),
  code: z.string(),
  totalAmount: z.number(),
  status: z.number(),
  note: z.string().nullable(),
  passengers: z.array(agentPassengerSchema),
});
export type AgentBooking = z.infer<typeof agentBookingSchema>;

export const agentBookingSummarySchema = z.object({
  id: z.string().uuid(),
  agentId: z.string().uuid(),
  quoteRequestId: z.string().uuid(),
  code: z.string(),
  totalAmount: z.number(),
  status: z.number(),
});
export type AgentBookingSummary = z.infer<typeof agentBookingSummarySchema>;

export const createAgentBookingFormSchema = z.object({
  quoteRequestId: z.string().min(1, 'Bắt buộc'),
  code: z.string(),
  note: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
});
export type CreateAgentBookingForm = z.infer<typeof createAgentBookingFormSchema>;

export const addPassengerFormSchema = z.object({
  fullName: z.string().min(1, 'Bắt buộc'),
  dateOfBirth: z.string().nullable(),
  passportNo: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  nationality: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  note: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
});
export type AddPassengerForm = z.infer<typeof addPassengerFormSchema>;
