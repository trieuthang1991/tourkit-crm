import { describe, expect, it } from 'vitest';
import { tourTemplateCreateSchema, tourTemplateSchema } from './types';

describe('tour template schemas', () => {
  it('parses a tour template', () => {
    const t = tourTemplateSchema.parse({
      id: crypto.randomUUID(),
      code: 'TPL001',
      title: 'Đà Lạt 3N2Đ',
      tourType: null,
      totalSlots: 40,
      reservationHours: 24,
      priceAdult: 2500000,
      priceChild: 1800000,
      priceChildSmall: 1200000,
      priceBaby: 0,
      termsNote: null,
      status: 1,
    });
    expect(t.code).toBe('TPL001');
  });

  it('create schema requires code and title', () => {
    expect(
      tourTemplateCreateSchema.safeParse({
        code: '',
        title: '',
        tourType: null,
        totalSlots: 40,
        reservationHours: 24,
        priceAdult: 0,
        priceChild: 0,
        priceChildSmall: 0,
        priceBaby: 0,
        termsNote: null,
      }).success,
    ).toBe(false);
  });
});
