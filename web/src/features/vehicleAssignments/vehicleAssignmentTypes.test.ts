import { describe, expect, it } from 'vitest';
import { vehicleAssignmentCreateSchema, vehicleAssignmentSchema } from './vehicleAssignmentTypes';

describe('vehicle assignment schemas', () => {
  it('parses a vehicle assignment', () => {
    const v = vehicleAssignmentSchema.parse({
      id: crypto.randomUUID(),
      tourDepartureId: crypto.randomUUID(),
      vehicleId: crypto.randomUUID(),
      driverName: 'Anh Ba',
      driverPhone: '0900000000',
      timeGo: null,
      timeCome: null,
      note: 'Xe chính',
      status: 1,
    });
    expect(v.driverName).toBe('Anh Ba');
  });

  it('create requires departure and vehicle', () => {
    expect(
      vehicleAssignmentCreateSchema.safeParse({
        tourDepartureId: '',
        vehicleId: '',
        driverName: null,
        driverPhone: null,
        timeGo: null,
        timeCome: null,
        note: null,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
