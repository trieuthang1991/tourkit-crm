import { describe, expect, it } from 'vitest';
import { providerCreateSchema, providerSchema } from './types';

describe('provider schemas', () => {
  it('parses a provider', () => {
    const p = providerSchema.parse({
      id: crypto.randomUUID(),
      code: 'NCC001',
      name: 'Khách sạn A',
      type: 1,
      phone: null,
      email: null,
      address: null,
      taxCode: null,
      contactPerson: null,
      bankAccount: null,
      bankName: null,
      paymentTermId: null,
      rate: 0,
      status: 1,
      province: null,
      branchId: null,
      totalCost: 0,
      paid: 0,
      outstanding: 0,
    });
    expect(p.code).toBe('NCC001');
  });

  it('create schema requires code and name', () => {
    expect(
      providerCreateSchema.safeParse({
        code: '',
        name: '',
        type: 1,
        phone: null,
        email: null,
        address: null,
        taxCode: null,
        contactPerson: null,
        bankAccount: null,
        bankName: null,
      paymentTermId: null,
        rate: 0,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
