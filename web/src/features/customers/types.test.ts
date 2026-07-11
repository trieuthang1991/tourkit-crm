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
    });
    expect(c.fullName).toBe('Nguyễn A');
    expect(c.customerType).toBe(1);
    expect(c.tempBalance).toBe(500000);
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
