import { describe, expect, it } from 'vitest';
import { approvalMethodLabel, approvalProcessDetailSchema, approvalProcessSchema } from './types';

describe('approval process schemas', () => {
  it('parses a process summary', () => {
    const p = approvalProcessSchema.parse({
      id: crypto.randomUUID(),
      name: 'Duyệt chi trên 10 triệu',
      method: 2,
      status: 0,
      stepCount: 3,
    });
    expect(p.name).toBe('Duyệt chi trên 10 triệu');
    expect(p.stepCount).toBe(3);
  });

  it('parses a detail with steps and users', () => {
    const detail = approvalProcessDetailSchema.parse({
      id: crypto.randomUUID(),
      name: 'QT',
      method: 1,
      status: 0,
      steps: [
        {
          id: crypto.randomUUID(),
          stepOrder: 1,
          positionId: crypto.randomUUID(),
          positionName: 'Kế toán trưởng',
          userIds: [crypto.randomUUID()],
          userNames: ['Nguyễn Văn A'],
        },
      ],
    });
    expect(detail.steps).toHaveLength(1);
    expect(detail.steps[0]!.positionName).toBe('Kế toán trưởng');
    expect(detail.steps[0]!.userNames[0]).toBe('Nguyễn Văn A');
  });

  it('maps method labels', () => {
    expect(approvalMethodLabel(1)).toContain('Một người');
    expect(approvalMethodLabel(2)).toContain('Tất cả');
  });
});
