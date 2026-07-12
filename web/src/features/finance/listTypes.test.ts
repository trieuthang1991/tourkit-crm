import { describe, expect, it } from 'vitest';
import { paymentListItemSchema, receiptListItemSchema, VOUCHER_STATUS, voucherStatusColor } from './listTypes';

describe('finance list types', () => {
  it('parses a receipt list row', () => {
    const r = receiptListItemSchema.parse({
      id: crypto.randomUUID(),
      code: 'RCP-ABC',
      orderId: crypto.randomUUID(),
      orderCode: 'ORD-1',
      customerName: 'Nguyễn Văn A',
      amount: 2_000_000,
      paymentMethod: 'cash',
      issuedAt: new Date().toISOString(),
      partner: null,
      status: 0,
      isRecognized: false,
    });
    expect(r.customerName).toBe('Nguyễn Văn A');
    expect(r.orderCode).toBe('ORD-1');
  });

  it('parses a payment list row with provider', () => {
    const p = paymentListItemSchema.parse({
      id: crypto.randomUUID(),
      code: 'PAY-XYZ',
      orderId: crypto.randomUUID(),
      orderCode: 'ORD-2',
      providerId: crypto.randomUUID(),
      providerName: 'Khách sạn A',
      amount: 1_500_000,
      paymentMethod: 'transfer',
      issuedAt: new Date().toISOString(),
      partner: null,
      receiverName: 'Kế toán',
      status: 1,
      isRecognized: true,
    });
    expect(p.providerName).toBe('Khách sạn A');
  });

  it('maps status labels and colors (backend 0/1/2)', () => {
    expect(VOUCHER_STATUS[0]).toBe('Chờ duyệt');
    expect(VOUCHER_STATUS[1]).toBe('Đã duyệt');
    expect(VOUCHER_STATUS[2]).toBe('Từ chối');
    expect(voucherStatusColor(1)).toBe('green');
    expect(voucherStatusColor(2)).toBe('red');
    expect(voucherStatusColor(0)).toBe('gold');
  });
});
