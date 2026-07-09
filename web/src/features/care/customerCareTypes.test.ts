import { describe, expect, it } from 'vitest';
import { customerCareCreateSchema, customerCareSchema } from './customerCareTypes';

describe('customer care schemas', () => {
  it('parses a customer care', () => {
    const c = customerCareSchema.parse({
      id: crypto.randomUUID(),
      customerId: crypto.randomUUID(),
      title: 'Gọi hỏi thăm',
      detail: null,
      remindAt: null,
      feedback: null,
      assignedToUserId: null,
      status: 1,
    });
    expect(c.title).toBe('Gọi hỏi thăm');
  });

  it('create schema requires customerId and title', () => {
    expect(
      customerCareCreateSchema.safeParse({
        customerId: '',
        title: '',
        detail: null,
        remindAt: null,
        assignedToUserId: null,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
