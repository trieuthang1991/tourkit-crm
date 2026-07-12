import { z } from 'zod';

// Trạng thái phiếu thu/chi (một cấp) — ĐÚNG theo backend: Create=0, Approve=1, Reject=2.
export const VOUCHER_STATUS: Record<number, string> = {
  0: 'Chờ duyệt',
  1: 'Đã duyệt',
  2: 'Từ chối',
};
export const voucherStatusColor = (s: number) => (s === 1 ? 'green' : s === 2 ? 'red' : 'gold');

export const receiptListItemSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  orderId: z.string().uuid(),
  orderCode: z.string().nullable(),
  customerName: z.string().nullable(),
  amount: z.number(),
  paymentMethod: z.string(),
  issuedAt: z.string(),
  partner: z.string().nullable(),
  status: z.number(),
  isRecognized: z.boolean(),
});
export type ReceiptListItem = z.infer<typeof receiptListItemSchema>;

export const paymentListItemSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  orderId: z.string().uuid(),
  orderCode: z.string().nullable(),
  providerId: z.string().uuid().nullable(),
  providerName: z.string().nullable(),
  amount: z.number(),
  paymentMethod: z.string(),
  issuedAt: z.string(),
  partner: z.string().nullable(),
  receiverName: z.string().nullable(),
  status: z.number(),
  isRecognized: z.boolean(),
});
export type PaymentListItem = z.infer<typeof paymentListItemSchema>;
