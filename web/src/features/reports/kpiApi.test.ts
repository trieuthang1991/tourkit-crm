import { describe, expect, it } from 'vitest';
import { kpiSummarySchema } from './kpiApi';

describe('kpiSummarySchema', () => {
  it('parses a kpi summary', () => {
    const k = kpiSummarySchema.parse({
      quoteCount: 3,
      quoteAcceptedCount: 2,
      quoteConvertedCount: 1,
      acceptanceRate: 0.6667,
      conversionRate: 0.5,
      orderCount: 1,
      totalRevenue: 13000000,
      avgOrderValue: 13000000,
      totalReceived: 5000000,
      collectionRate: 0.3846,
    });
    expect(k.quoteCount).toBe(3);
    expect(k.conversionRate).toBe(0.5);
  });
});
