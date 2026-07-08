import { describe, expect, it } from 'vitest';
import { decodeToken } from './jwt';

// Payload: { perm: ['customer.view','tour.view'], email: 'a@b.c', tenant_id: 't1' }
const TOKEN =
  'x.' +
  btoa(JSON.stringify({ perm: ['customer.view', 'tour.view'], email: 'a@b.c', tenant_id: 't1' })) +
  '.y';

describe('decodeToken', () => {
  it('reads permissions and email', () => {
    const claims = decodeToken(TOKEN);
    expect(claims?.permissions).toContain('customer.view');
    expect(claims?.email).toBe('a@b.c');
  });

  it('returns null for garbage', () => {
    expect(decodeToken('not-a-jwt')).toBeNull();
  });

  it('normalizes a single string perm claim to an array', () => {
    const t = 'x.' + btoa(JSON.stringify({ perm: 'customer.view' })) + '.y';
    expect(decodeToken(t)?.permissions).toEqual(['customer.view']);
  });
});
