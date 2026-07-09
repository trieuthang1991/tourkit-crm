import { describe, expect, it } from 'vitest';
import { commissionRuleCreateSchema, commissionRuleSchema } from './commissionRuleTypes';

describe('commissionRule schemas', () => {
  it('parses a commission rule', () => {
    const r = commissionRuleSchema.parse({
      id: crypto.randomUUID(),
      userId: crypto.randomUUID(),
      percentage: 10,
      status: 1,
    });
    expect(r.percentage).toBe(10);
  });

  it('create schema requires a valid userId', () => {
    expect(
      commissionRuleCreateSchema.safeParse({
        userId: 'not-a-uuid',
        percentage: 10,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
