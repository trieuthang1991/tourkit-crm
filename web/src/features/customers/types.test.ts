import { describe, expect, it } from 'vitest';
import { customerFormSchema, customerSchema } from './types';

describe('customer schemas', () => {
  it('parses a customer', () => {
    const c = customerSchema.parse({ id: crypto.randomUUID(), fullName: 'Nguyễn A', phone: null });
    expect(c.fullName).toBe('Nguyễn A');
  });
  it('form requires fullName', () => {
    expect(customerFormSchema.safeParse({ fullName: '', phone: '' }).success).toBe(false);
  });
});
