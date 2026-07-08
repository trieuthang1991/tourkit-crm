import { describe, expect, it } from 'vitest';
import { departureFormSchema, departureSchema } from './departureTypes';

describe('departure schemas', () => {
  it('parses a departure', () => {
    const d = departureSchema.parse({
      id: crypto.randomUUID(),
      code: 'DEP001',
      title: 'Đà Lạt 3N2Đ - T7',
      templateId: crypto.randomUUID(),
      departureDate: '2026-08-01T00:00:00Z',
      endDate: '2026-08-03T00:00:00Z',
      totalSlots: 40,
      status: 1,
    });
    expect(d.code).toBe('DEP001');
  });

  it('form requires code and title', () => {
    expect(
      departureFormSchema.safeParse({
        templateId: null,
        code: '',
        title: '',
        departureDate: null,
        endDate: null,
        totalSlots: 40,
      }).success,
    ).toBe(false);
  });

  it('form parses nullable dates', () => {
    const f = departureFormSchema.parse({
      templateId: null,
      code: 'DEP001',
      title: 'Đà Lạt 3N2Đ',
      departureDate: null,
      endDate: null,
      totalSlots: 40,
    });
    expect(f.departureDate).toBeNull();
  });
});
