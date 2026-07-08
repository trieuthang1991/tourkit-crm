import { describe, expect, it } from 'vitest';
import { approvalSchema, approvalStepSchema } from './approvalTypes';

describe('approvalStepSchema', () => {
  it('parses a pending step', () => {
    const step = approvalStepSchema.parse({
      stepOrder: 1,
      userId: crypto.randomUUID(),
      status: 1,
      actedAt: null,
      note: null,
    });
    expect(step.status).toBe(1);
    expect(step.actedAt).toBeNull();
  });

  it('parses an acted step with a note', () => {
    const step = approvalStepSchema.parse({
      stepOrder: 2,
      userId: crypto.randomUUID(),
      status: 2,
      actedAt: new Date().toISOString(),
      note: 'Đồng ý',
    });
    expect(step.note).toBe('Đồng ý');
  });
});

describe('approvalSchema', () => {
  it('parses an approval with steps', () => {
    const a = approvalSchema.parse({
      id: crypto.randomUUID(),
      receiptVoucherId: crypto.randomUUID(),
      method: 1,
      currentStepOrder: 1,
      status: 1,
      steps: [{ stepOrder: 1, userId: crypto.randomUUID(), status: 1, actedAt: null, note: null }],
    });
    expect(a.steps).toHaveLength(1);
    expect(a.method).toBe(1);
  });
});
