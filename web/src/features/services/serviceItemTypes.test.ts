import { describe, expect, it } from 'vitest';
import { serviceItemCreateSchema, serviceItemSchema } from './serviceItemTypes';

describe('service item schemas', () => {
  it('parses a service item', () => {
    const s = serviceItemSchema.parse({
      id: crypto.randomUUID(),
      code: 'DV001',
      name: 'Khách sạn 3 sao',
      category: 1,
      status: 1,
    });
    expect(s.code).toBe('DV001');
  });

  it('create schema requires code and name', () => {
    expect(
      serviceItemCreateSchema.safeParse({
        code: '',
        name: '',
        category: 1,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
