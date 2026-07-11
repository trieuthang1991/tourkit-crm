import { describe, expect, it } from 'vitest';
import { guideSettlementSchema } from './guideTransactionApi';

describe('guideSettlementSchema', () => {
  it('parses a settlement with items', () => {
    const s = guideSettlementSchema.parse({
      totalRevenue: 2000000,
      totalExpense: 800000,
      net: 1200000,
      items: [
        {
          id: crypto.randomUUID(),
          tourGuideAssignmentId: crypto.randomUUID(),
          type: 0,
          amount: 2000000,
          description: 'Bán thêm vé',
          occurredAt: new Date().toISOString(),
        },
      ],
    });
    expect(s.net).toBe(1200000);
    expect(s.items).toHaveLength(1);
  });
});
