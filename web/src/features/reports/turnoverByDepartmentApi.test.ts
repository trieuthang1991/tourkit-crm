import { describe, expect, it } from 'vitest';
import { turnoverByDepartmentRowSchema } from './turnoverByDepartmentApi';

describe('turnoverByDepartmentRowSchema', () => {
  it('parses a department row', () => {
    const r = turnoverByDepartmentRowSchema.parse({
      departmentId: crypto.randomUUID(),
      departmentName: 'Điều hành',
      orderCount: 3,
      turnover: 39000000,
      cost: 20000000,
      profit: 19000000,
    });
    expect(r.departmentName).toBe('Điều hành');
    expect(r.orderCount).toBe(3);
  });

  it('allows null departmentId (unassigned)', () => {
    const r = turnoverByDepartmentRowSchema.parse({
      departmentId: null,
      departmentName: 'Chưa phân bổ',
      orderCount: 1,
      turnover: 13000000,
      cost: 0,
      profit: 13000000,
    });
    expect(r.departmentId).toBeNull();
  });
});
