import { describe, expect, it } from 'vitest';
import { paymentAccountCreateSchema, paymentAccountSchema } from './types';

describe('payment account schemas', () => {
  it('parses an account with bank fields', () => {
    const a = paymentAccountSchema.parse({
      id: crypto.randomUUID(),
      name: 'VCB - Cty ABC',
      bankName: 'Vietcombank',
      accountNumber: '0123456789',
      accountHolder: 'CÔNG TY ABC',
      branch: 'CN Hà Nội',
      transferNote: 'Thanh toan tour',
      isDefault: true,
      sortOrder: 1,
      status: 1,
    });
    expect(a.bankName).toBe('Vietcombank');
    expect(a.isDefault).toBe(true);
  });

  it('create form requires name and coerces empty text to null', () => {
    const parsed = paymentAccountCreateSchema.parse({
      name: 'VCB',
      bankName: '',
      accountNumber: '0123',
      accountHolder: '',
      branch: '',
      transferNote: '',
      isDefault: false,
      sortOrder: 0,
    });
    expect(parsed.bankName).toBeNull();
    expect(parsed.accountNumber).toBe('0123');

    expect(
      paymentAccountCreateSchema.safeParse({
        name: '',
        bankName: null,
        accountNumber: null,
        accountHolder: null,
        branch: null,
        transferNote: null,
        isDefault: false,
        sortOrder: 0,
      }).success,
    ).toBe(false);
  });
});
