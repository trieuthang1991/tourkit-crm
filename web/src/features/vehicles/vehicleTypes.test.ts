import { describe, expect, it } from 'vitest';
import { vehicleCreateSchema, vehicleSchema } from './vehicleTypes';

describe('vehicle schemas', () => {
  it('parses a vehicle', () => {
    const v = vehicleSchema.parse({
      id: crypto.randomUUID(),
      name: 'Xe 16 chỗ',
      firmName: 'Toyota',
      seatType: 16,
      status: 1,
    });
    expect(v.name).toBe('Xe 16 chỗ');
    expect(v.seatType).toBe(16);
  });

  it('create requires name', () => {
    expect(
      vehicleCreateSchema.safeParse({ name: '', firmName: null, seatType: 4, status: 1 }).success,
    ).toBe(false);
  });
});
