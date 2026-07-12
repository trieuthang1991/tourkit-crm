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

// Trạng thái phiếu thu ĐÚNG backend: Create=0 (chờ duyệt), Approve=1, Reject=2 (giống VOUCHER_STATUS).
export const RECEIPT_STATUS: Record<number, string> = {
  0: 'Chờ duyệt',
  1: 'Đã duyệt',
  2: 'Từ chối',
};
