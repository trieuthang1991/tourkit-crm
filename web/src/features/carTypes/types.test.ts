import { describe, expect, it } from 'vitest';
import { carTypeCreateSchema, carTypeSchema } from './types';

describe('car type schemas', () => {
  it('parses a car type', () => {
    const t = carTypeSchema.parse({ id: crypto.randomUUID(), code: 45, name: 'Xe 45 chỗ', sortOrder: 1, status: 1 });
    expect(t.code).toBe(45);
    expect(t.name).toBe('Xe 45 chỗ');
  });

  it('create form requires positive code and name', () => {
    expect(carTypeCreateSchema.safeParse({ code: 0, name: 'Xe', sortOrder: 0 }).success).toBe(false);
    expect(carTypeCreateSchema.safeParse({ code: 16, name: '', sortOrder: 0 }).success).toBe(false);
    expect(carTypeCreateSchema.safeParse({ code: 16, name: 'Xe 16 chỗ', sortOrder: 0 }).success).toBe(true);
  });
});
