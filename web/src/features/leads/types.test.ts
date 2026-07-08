import { describe, expect, it } from 'vitest';
import { leadCreateSchema, leadSchema } from './types';

describe('lead schemas', () => {
  it('parses a lead', () => {
    const l = leadSchema.parse({
      id: crypto.randomUUID(),
      fullName: 'Trần B',
      phone: null,
      email: null,
      source: null,
      status: 1,
      assignedToUserId: null,
      convertedCustomerId: null,
    });
    expect(l.status).toBe(1);
  });

  it('create schema rejects empty fullName', () => {
    expect(
      leadCreateSchema.safeParse({ fullName: '', phone: null, email: null, source: null, assignedToUserId: null })
        .success,
    ).toBe(false);
  });
});
