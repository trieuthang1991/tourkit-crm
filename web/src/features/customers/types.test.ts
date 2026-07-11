import { describe, expect, it } from 'vitest';
import { customerFormSchema, customerSchema } from './types';

describe('customer schemas', () => {
  it('parses a customer', () => {
    const c = customerSchema.parse({
      id: crypto.randomUUID(),
      fullName: 'Nguyễn A',
      phone: null,
      customerType: 1,
      source: 'Facebook',
      tag: 'VIP',
      tempBalance: 500000,
      email: 'a@x.com',
      address: 'Hà Nội',
      dateOfBirth: null,
      idCardNumber: '0123',
      passportNumber: 'B123',
      passportExpiry: null,
      nationality: 'Việt Nam',
    });
    expect(c.fullName).toBe('Nguyễn A');
    expect(c.passportNumber).toBe('B123');
    expect(c.nationality).toBe('Việt Nam');
  });

  it('form requires fullName', () => {
    expect(
      customerFormSchema.safeParse({
        fullName: '',
        phone: '',
        customerType: 0,
        source: '',
        tag: '',
        tempBalance: 0,
      }).success,
    ).toBe(false);
  });
});
