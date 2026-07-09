import { describe, expect, it } from 'vitest';
import { tourRatingCreateSchema, tourRatingSchema } from './tourRatingTypes';

describe('tour rating schemas', () => {
  it('parses a tour rating', () => {
    const r = tourRatingSchema.parse({
      id: crypto.randomUUID(),
      tourDepartureId: null,
      orderId: null,
      customerName: 'Nguyễn Văn A',
      customerPhone: null,
      stars: 5,
      comment: null,
      status: 1,
    });
    expect(r.stars).toBe(5);
  });

  it('create schema rejects stars out of range', () => {
    expect(
      tourRatingCreateSchema.safeParse({
        tourDepartureId: null,
        orderId: null,
        customerName: null,
        customerPhone: null,
        stars: 0,
        comment: null,
        status: 1,
      }).success,
    ).toBe(false);
    expect(
      tourRatingCreateSchema.safeParse({
        tourDepartureId: null,
        orderId: null,
        customerName: null,
        customerPhone: null,
        stars: 6,
        comment: null,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
