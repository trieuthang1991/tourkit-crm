import { z } from 'zod';

export const receiptSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  orderId: z.string().uuid(),
  amount: z.number(),
  paymentMethod: z.string(),
  issuedAt: z.string(),
  partner: z.string().nullable(),
  note: z.string().nullable(),
  status: z.number(),
  isRecognized: z.boolean(),
});
export type Receipt = z.infer<typeof receiptSchema>;

export const balanceSchema = z.object({
  orderId: z.string().uuid(),
  total: z.number(),
  paid: z.number(),
  outstanding: z.number(),
});
export type Balance = z.infer<typeof balanceSchema>;

export type CreateReceiptForm = {
  amount: number;
  paymentMethod: string;
  partner: string | null;
  note: string | null;
};

// status:1 = Chờ duyệt (pending) — approve()/reject() chuyển sang 2/3.
export const RECEIPT_STATUS: Record<number, string> = {
  1: 'Chờ duyệt',
  2: 'Đã duyệt',
  3: 'Từ chối',
};
