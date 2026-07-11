import { describe, expect, it } from 'vitest';
import { roomClassCreateSchema, roomClassSchema } from './types';

describe('room class schemas', () => {
  it('parses a room class', () => {
    const r = roomClassSchema.parse({ id: crypto.randomUUID(), name: 'Deluxe', sortOrder: 1, status: 1 });
    expect(r.name).toBe('Deluxe');
  });

  it('create form requires name', () => {
    expect(roomClassCreateSchema.safeParse({ name: '', sortOrder: 0 }).success).toBe(false);
    expect(roomClassCreateSchema.safeParse({ name: 'Suite', sortOrder: 0 }).success).toBe(true);
  });
});
