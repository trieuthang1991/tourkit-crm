import { describe, expect, it } from 'vitest';
import { providerServiceCreateSchema, providerServiceSchema } from './providerServiceTypes';

describe('provider service schemas', () => {
  it('parses a provider service', () => {
    const p = providerServiceSchema.parse({
      id: crypto.randomUUID(),
      providerId: crypto.randomUUID(),
      serviceItemId: null,
      priceName: null,
      contractPrice: 100000,
      publicPrice: 150000,
      amountOfPeople: 1,
      note: null,
      status: 1,
    });
    expect(p.contractPrice).toBe(100000);
  });

  it('create schema requires a valid providerId', () => {
    expect(
      providerServiceCreateSchema.safeParse({
        providerId: 'not-a-uuid',
        serviceItemId: null,
        priceName: null,
        contractPrice: 100000,
        publicPrice: 150000,
        amountOfPeople: 1,
        note: null,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
