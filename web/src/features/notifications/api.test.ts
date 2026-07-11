import { describe, expect, it } from 'vitest';
import { notificationSchema } from './api';

describe('notificationSchema', () => {
  it('parses a notification', () => {
    const n = notificationSchema.parse({
      id: crypto.randomUUID(),
      title: 'Bạn được giao công việc',
      message: 'Gọi khách',
      linkUrl: '/work-tasks',
      isRead: false,
      createdAt: new Date().toISOString(),
    });
    expect(n.title).toBe('Bạn được giao công việc');
    expect(n.isRead).toBe(false);
  });

  it('allows null message and link', () => {
    const n = notificationSchema.parse({
      id: crypto.randomUUID(),
      title: 'X',
      message: null,
      linkUrl: null,
      isRead: true,
      createdAt: new Date().toISOString(),
    });
    expect(n.message).toBeNull();
  });
});
