import { describe, expect, it } from 'vitest';
import { departmentCreateSchema, departmentSchema, positionCreateSchema, positionSchema } from './types';

describe('org catalog schemas', () => {
  it('parses a department and position', () => {
    const d = departmentSchema.parse({ id: crypto.randomUUID(), name: 'Điều hành', code: 'DH', sortOrder: 1, status: 1 });
    expect(d.name).toBe('Điều hành');
    const p = positionSchema.parse({ id: crypto.randomUUID(), name: 'Trưởng phòng', sortOrder: 1, status: 1 });
    expect(p.name).toBe('Trưởng phòng');
  });

  it('create forms require name; department coerces empty code to null', () => {
    expect(departmentCreateSchema.parse({ name: 'KD', code: '', sortOrder: 0 }).code).toBeNull();
    expect(departmentCreateSchema.safeParse({ name: '', code: null, sortOrder: 0 }).success).toBe(false);
    expect(positionCreateSchema.safeParse({ name: '', sortOrder: 0 }).success).toBe(false);
  });
});
