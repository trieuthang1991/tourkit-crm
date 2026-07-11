import { describe, expect, it } from 'vitest';
import { workTaskFormSchema, workTaskSchema } from './types';

describe('work task schemas', () => {
  it('parses a work task', () => {
    const t = workTaskSchema.parse({
      id: crypto.randomUUID(),
      title: 'Gọi khách',
      description: null,
      assigneeUserId: null,
      assigneeName: null,
      dueDate: null,
      priority: 1,
      status: 0,
      relatedOrderId: null,
    });
    expect(t.title).toBe('Gọi khách');
    expect(t.status).toBe(0);
  });

  it('form requires a title', () => {
    expect(
      workTaskFormSchema.safeParse({
        title: '',
        description: null,
        assigneeUserId: null,
        dueDate: null,
        priority: 1,
        status: 0,
        relatedOrderId: null,
      }).success,
    ).toBe(false);
  });
});
