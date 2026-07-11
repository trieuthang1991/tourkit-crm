import { describe, expect, it } from 'vitest';
import { tourTransferSchema } from './transferApi';

describe('tourTransferSchema', () => {
  it('parses a transfer history entry', () => {
    const t = tourTransferSchema.parse({
      id: crypto.randomUUID(),
      orderId: crypto.randomUUID(),
      fromDepartureId: crypto.randomUUID(),
      toDepartureId: crypto.randomUUID(),
      reason: 'Khách kẹt lịch',
      transferredAt: new Date().toISOString(),
    });
    expect(t.reason).toBe('Khách kẹt lịch');
  });

  it('allows null reason', () => {
    const t = tourTransferSchema.parse({
      id: crypto.randomUUID(),
      orderId: crypto.randomUUID(),
      fromDepartureId: crypto.randomUUID(),
      toDepartureId: crypto.randomUUID(),
      reason: null,
      transferredAt: new Date().toISOString(),
    });
    expect(t.reason).toBeNull();
  });
});
