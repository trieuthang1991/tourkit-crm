import { z } from 'zod';

export const quoteLineSchema = z.object({
  id: z.string(),
  description: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  amount: z.number(),
  serviceType: z.number(),
  scope: z.number(), // 0 cả đoàn, 1 theo khách
  providerServiceId: z.string().nullable(),
  unitCost: z.number(),
  marginPercent: z.number(),
});

export const quoteSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  customerId: z.string().nullable(),
  customerName: z.string(),
  title: z.string(),
  validUntil: z.string().nullable(),
  status: z.number(),
  note: z.string().nullable(),
  totalAmount: z.number(),
  lines: z.array(quoteLineSchema),
  // Dự trù giá (spec 2026-07-11)
  adults: z.number(),
  children: z.number(),
  infants: z.number(),
  childPercent: z.number(),
  infantPercent: z.number(),
  totalCost: z.number(),
  totalProfit: z.number(),
  adultPrice: z.number(),
  childPrice: z.number(),
  infantPrice: z.number(),
  convertedOrderId: z.string().nullable(), // đơn đã sinh từ báo giá (null = chưa chuyển)
});
export type Quote = z.infer<typeof quoteSchema>;

export const quoteSummarySchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  customerName: z.string(),
  title: z.string(),
  validUntil: z.string().nullable(),
  status: z.number(),
  totalAmount: z.number(),
  convertedOrderId: z.string().nullable(), // đơn đã sinh — hiện nút mở đơn để thu tiền (BillPaymentRequest = flow phiếu thu sẵn có)
  adults: z.number().optional(),
  children: z.number().optional(),
  infants: z.number().optional(),
  totalCost: z.number().optional(),
  totalProfit: z.number().optional(),
});
export type QuoteSummary = z.infer<typeof quoteSummarySchema>;

export const quoteFormSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  customerName: z.string(),
  title: z.string().min(1, 'Bắt buộc'),
  validUntil: z.string().nullable(),
  status: z.number(),
  note: z
    .string()
    .nullable()
    .transform((v) => (v ? v : null)),
  adults: z.number().min(0),
  children: z.number().min(0),
  infants: z.number().min(0),
  childPercent: z.number().min(0).max(100),
  infantPercent: z.number().min(0).max(100),
  lines: z
    .array(
      z.object({
        description: z.string().min(1, 'Bắt buộc'),
        quantity: z.number(),
        unitPrice: z.number(),
        serviceType: z.number(),
        scope: z.number(),
        providerServiceId: z.string().nullable(),
        unitCost: z.number().min(0),
        marginPercent: z.number().min(0).max(500),
      }),
    )
    .min(1, 'Cần ít nhất 1 dòng'),
});
export type QuoteForm = z.infer<typeof quoteFormSchema>;

export const SERVICE_TYPE_OPTIONS = [
  { value: 0, label: 'Khác' },
  { value: 1, label: 'Khách sạn' },
  { value: 2, label: 'Xe' },
  { value: 3, label: 'HDV' },
  { value: 4, label: 'Ăn uống' },
  { value: 5, label: 'Vé tham quan' },
  { value: 6, label: 'Visa' },
  { value: 7, label: 'Vé máy bay' },
  { value: 8, label: 'Bảo hiểm' },
];

export const SCOPE_OPTIONS = [
  { value: 0, label: 'Cả đoàn' },
  { value: 1, label: 'Theo khách' },
];
