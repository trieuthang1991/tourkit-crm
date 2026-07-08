import { describe, expect, it } from 'vitest';
import { planSchema, subscriptionSchema } from './billingTypes';

describe('planSchema', () => {
  it('parses a plan', () => {
    const p = planSchema.parse({
      id: crypto.randomUUID(),
      code: 'pro',
      name: 'Gói Pro',
      maxUsers: 20,
      maxTours: 100,
      priceMonthly: 990000,
    });
    expect(p.code).toBe('pro');
    expect(p.priceMonthly).toBe(990000);
  });
});

describe('subscriptionSchema', () => {
  it('parses an active subscription', () => {
    const s = subscriptionSchema.parse({
      id: crypto.randomUUID(),
      planId: crypto.randomUUID(),
      planCode: 'pro',
      status: 1,
      startedAt: new Date().toISOString(),
      expiresAt: null,
    });
    expect(s.status).toBe(1);
    expect(s.expiresAt).toBeNull();
  });

  it('parses an expiring subscription', () => {
    const s = subscriptionSchema.parse({
      id: crypto.randomUUID(),
      planId: crypto.randomUUID(),
      planCode: 'basic',
      status: 2,
      startedAt: new Date().toISOString(),
      expiresAt: new Date().toISOString(),
    });
    expect(s.status).toBe(2);
    expect(s.expiresAt).not.toBeNull();
  });
});
