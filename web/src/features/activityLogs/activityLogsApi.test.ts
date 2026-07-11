import { describe, expect, it } from 'vitest';
import { activityLogSchema } from './activityLogsApi';

describe('activity log schema', () => {
  it('parses an activity log', () => {
    const log = activityLogSchema.parse({
      id: crypto.randomUUID(),
      userId: crypto.randomUUID(),
      action: 'Update',
      entityName: 'Customer',
      entityId: 'c1',
      changes: '{"FullName":{"old":"A","new":"B"}}',
      createdAt: new Date().toISOString(),
    });
    expect(log.action).toBe('Update');
    expect(log.entityName).toBe('Customer');
  });

  it('allows null userId and changes', () => {
    const log = activityLogSchema.parse({
      id: crypto.randomUUID(),
      userId: null,
      action: 'Insert',
      entityName: 'Order',
      entityId: 'o1',
      changes: null,
      createdAt: new Date().toISOString(),
    });
    expect(log.userId).toBeNull();
  });
});
