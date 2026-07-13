import { z } from 'zod';

// Vé máy bay lẻ — DTO trả từ API (P/L derived).
export const flightIndividualSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  ticketCode: z.string().nullable(),
  pnr: z.string(),
  customerName: z.string(),
  orderRef: z.string().nullable(),
  orderCode: z.string().nullable(),
  providerRef: z.string().nullable(),
  providerName: z.string().nullable(),
  tripType: z.number(),
  route: z.string().nullable(),
  departDate: z.string().nullable(),
  returnDate: z.string().nullable(),
  sellAmount: z.number(),
  receivedAmount: z.number(),
  receivableRemaining: z.number(),
  totalCost: z.number(),
  paidAmount: z.number(),
  payableRemaining: z.number(),
  profit: z.number(),
  paymentDueDate: z.string().nullable(),
  status: z.number(),
  assigneeRef: z.string().nullable(),
  note: z.string().nullable(),
});
export type FlightIndividual = z.infer<typeof flightIndividualSchema>;

export const flightIndividualStatsSchema = z.object({
  total: z.number(),
  new: z.number(),
  approved: z.number(),
  rejected: z.number(),
  pendingPay: z.number(),
  dueSoon: z.number(),
  overdue: z.number(),
  partialPay: z.number(),
  success: z.number(),
  partialReceive: z.number(),
  totalSell: z.number(),
  totalReceived: z.number(),
  totalReceivable: z.number(),
  totalCost: z.number(),
  totalPaid: z.number(),
  totalPayable: z.number(),
  totalProfit: z.number(),
});

export const flightIndividualFormSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  ticketCode: z.string().nullable(),
  pnr: z.string(),
  customerName: z.string().min(1, 'Bắt buộc'),
  orderRef: z.string().nullable(),
  providerRef: z.string().nullable(),
  tripType: z.number(),
  route: z.string().nullable(),
  departDate: z.string().nullable(),
  returnDate: z.string().nullable(),
  sellAmount: z.number().min(0),
  receivedAmount: z.number().min(0),
  totalCost: z.number().min(0),
  paidAmount: z.number().min(0),
  paymentDueDate: z.string().nullable(),
  status: z.number(),
  assigneeRef: z.string().nullable(),
  note: z.string().nullable(),
});
export type FlightIndividualForm = z.infer<typeof flightIndividualFormSchema>;

export const TRIP_TYPE_LABEL: Record<number, string> = { 0: 'Một chiều', 1: 'Khứ hồi' };
export const TRIP_TYPE_OPTIONS = [
  { value: 0, label: 'Một chiều' },
  { value: 1, label: 'Khứ hồi' },
];

export const FI_STATUS_LABEL: Record<number, string> = { 0: 'Tạo mới', 1: 'Đã duyệt', 2: 'Không duyệt' };
export const FI_STATUS_COLOR: Record<number, string> = { 0: 'blue', 1: 'green', 2: 'red' };
export const FI_STATUS_OPTIONS = [
  { value: 0, label: 'Tạo mới' },
  { value: 1, label: 'Đã duyệt' },
  { value: 2, label: 'Không duyệt' },
];

// Sub-tab bám hệ cũ (khớp Tab param backend).
export const FI_TABS: { value: string; label: string; statKey: keyof z.infer<typeof flightIndividualStatsSchema> }[] = [
  { value: 'new', label: 'Tạo mới', statKey: 'new' },
  { value: 'approved', label: 'Đã duyệt', statKey: 'approved' },
  { value: 'rejected', label: 'Không duyệt', statKey: 'rejected' },
  { value: 'pending-pay', label: 'Chờ chi', statKey: 'pendingPay' },
  { value: 'due-soon', label: 'Đến hạn chi 24h', statKey: 'dueSoon' },
  { value: 'overdue', label: 'Quá hạn chi', statKey: 'overdue' },
  { value: 'partial-pay', label: 'Chưa chi hết', statKey: 'partialPay' },
  { value: 'success', label: 'Thành công', statKey: 'success' },
  { value: 'partial-receive', label: 'Chưa thu hết', statKey: 'partialReceive' },
];
