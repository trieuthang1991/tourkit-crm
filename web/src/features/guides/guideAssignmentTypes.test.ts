import { describe, expect, it } from 'vitest';
import { guideAssignmentCreateSchema, guideAssignmentSchema } from './guideAssignmentTypes';

describe('guide assignment schemas', () => {
  it('parses a guide assignment', () => {
    const g = guideAssignmentSchema.parse({
      id: crypto.randomUUID(),
      tourDepartureId: crypto.randomUUID(),
      providerId: crypto.randomUUID(),
      timeGo: null,
      timeCome: null,
      timeReturn: null,
      note: 'HDV chính',
      status: 1,
      handoverContent: null,
      handedOverAt: null,
    });
    expect(g.note).toBe('HDV chính');
  });

  it('create requires departure and provider', () => {
    expect(
      guideAssignmentCreateSchema.safeParse({
        tourDepartureId: '',
        providerId: '',
        timeGo: null,
        timeCome: null,
        timeReturn: null,
        note: null,
        status: 1,
      handoverContent: null,
      handedOverAt: null,
      }).success,
    ).toBe(false);
  });
});
