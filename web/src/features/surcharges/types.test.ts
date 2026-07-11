import { describe, expect, it } from 'vitest';
import { surchargeCreateSchema, surchargeSchema } from './types';

describe('surcharge schemas', () => {
  it('parses a surcharge', () => {
    const s = surchargeSchema.parse({ id: crypto.randomUUID(), name: 'Cao điểm', calcType: 1, defaultValue: 10, sortOrder: 1, status: 1 });
    expect(s.calcType).toBe(1);
    expect(s.defaultValue).toBe(10);
  });

  it('create form requires name and valid calc type', () => {
    expect(surchargeCreateSchema.safeParse({ name: '', calcType: 0, defaultValue: 0, sortOrder: 0 }).success).toBe(false);
    expect(surchargeCreateSchema.safeParse({ name: 'X', calcType: 5, defaultValue: 0, sortOrder: 0 }).success).toBe(false);
    expect(surchargeCreateSchema.safeParse({ name: 'Phòng đơn', calcType: 0, defaultValue: 500000, sortOrder: 0 }).success).toBe(true);
  });
});
