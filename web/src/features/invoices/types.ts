import { z } from 'zod';

const nullableStr = z
  .string()
  .nullable()
  .transform((v) => (v ? v : null));

export const invoiceLineSchema = z.object({
  id: z.string(),
  description: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  vatRate: z.number(),
  lineAmount: z.number(),
  lineVat: z.number(),
});

export const invoiceSchema = z.object({
  id: z.string().uuid(),
  series: z.string(),
  number: z.string(),
  invoiceDate: z.string(),
  orderId: z.string().nullable(),
  buyerName: z.string(),
  buyerTaxCode: z.string().nullable(),
  buyerAddress: z.string().nullable(),
  subtotal: z.number(),
  vatAmount: z.number(),
  totalAmount: z.number(),
  status: z.number(),
  note: z.string().nullable(),
  lines: z.array(invoiceLineSchema),
});
export type Invoice = z.infer<typeof invoiceSchema>;

export const invoiceSummarySchema = z.object({
  id: z.string().uuid(),
  series: z.string(),
  number: z.string(),
  invoiceDate: z.string(),
  buyerName: z.string(),
  totalAmount: z.number(),
  status: z.number(),
});
export type InvoiceSummary = z.infer<typeof invoiceSummarySchema>;

export const invoiceFormSchema = z.object({
  series: z.string(),
  number: z.string(),
  invoiceDate: z.string().min(1, 'Bắt buộc'),
  buyerName: z.string().min(1, 'Bắt buộc'),
  buyerTaxCode: nullableStr,
  buyerAddress: nullableStr,
  status: z.number(),
  note: nullableStr,
  lines: z
    .array(
      z.object({
        description: z.string().min(1, 'Bắt buộc'),
        quantity: z.number(),
        unitPrice: z.number(),
        vatRate: z.number(),
      }),
    )
    .min(1, 'Cần ít nhất 1 dòng'),
});
export type InvoiceForm = z.infer<typeof invoiceFormSchema>;
