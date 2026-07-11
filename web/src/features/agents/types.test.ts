import { describe, expect, it } from 'vitest';
import { agentFormSchema, agentSchema } from './types';

describe('agent schemas', () => {
  it('parses an agent', () => {
    const a = agentSchema.parse({
      id: crypto.randomUUID(),
      code: 'AG-1',
      name: 'Đại lý ABC',
      contactPerson: null,
      phone: null,
      email: null,
      taxCode: null,
      address: null,
      creditLimit: 100000000,
      status: 1,
    });
    expect(a.name).toBe('Đại lý ABC');
    expect(a.creditLimit).toBe(100000000);
  });

  it('form requires code and name', () => {
    expect(
      agentFormSchema.safeParse({
        code: '',
        name: '',
        contactPerson: '',
        phone: '',
        email: '',
        taxCode: '',
        address: '',
        creditLimit: 0,
        status: 1,
      }).success,
    ).toBe(false);
  });
});
