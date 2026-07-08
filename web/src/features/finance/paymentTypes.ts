import { z } from 'zod';

export const paymentSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  orderId: z.string().uuid(),
  providerId: z.string().uuid().nullable(),
  orderCostId: z.string().uuid().nullable(),
  amount: z.number(),
  paymentMethod: z.string(),
  issuedAt: z.string(),
  partner: z.string().nullable(),
  receiverName: z.string().nullable(),
  note: z.string().nullable(),
  status: z.number(),
  isRecognized: z.boolean(),
});
export type Payment = z.infer<typeof paymentSchema>;

export type CreatePaymentForm = {
  providerId: string | null;
  orderCostId: string | null;
  amount: number;
  paymentMethod: string;
  partner: string | null;
  receiverName: string | null;
  note: string | null;
};

// status:0 = Chờ duyệt (pending) — approve()/reject() chuyển sang 1/2.
export const PAYMENT_STATUS: Record<number, string> = {
  0: 'Chờ duyệt',
  1: 'Đã duyệt',
  2: 'Từ chối',
};
