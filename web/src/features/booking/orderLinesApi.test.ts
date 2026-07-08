import { describe, expect, it } from 'vitest';
import { bookingLineSchema } from './orderLinesApi';

describe('bookingLineSchema', () => {
  it('parses a booking line', () => {
    const line = bookingLineSchema.parse({
      id: crypto.randomUUID(),
      quantity: 2,
      amountChildren: 1,
      amountChildrenSmall: 0,
      quantityBaby: 0,
      priceAdult: 2500000,
      priceChild: 1800000,
      priceChildSmall: 1200000,
      priceBaby: 0,
      upfrontAmount: 500000,
      reservationCode: 'RSV001',
      isMainContact: true,
    });
    expect(line.quantity).toBe(2);
    expect(line.isMainContact).toBe(true);
  });

  it('allows a null reservationCode', () => {
    const line = bookingLineSchema.parse({
      id: crypto.randomUUID(),
      quantity: 1,
      amountChildren: 0,
      amountChildrenSmall: 0,
      quantityBaby: 0,
      priceAdult: 2500000,
      priceChild: 0,
      priceChildSmall: 0,
      priceBaby: 0,
      upfrontAmount: 0,
      reservationCode: null,
      isMainContact: false,
    });
    expect(line.reservationCode).toBeNull();
  });
});
