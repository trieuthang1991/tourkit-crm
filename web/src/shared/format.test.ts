import { describe, expect, it } from 'vitest';
import { money, statusText } from './format';

describe('format', () => {
  it('formats money vi-VN', () => {
    expect(money(1500000)).toBe('1.500.000');
  });
  it('maps a status code to a label', () => {
    expect(statusText({ 1: 'Nháp', 2: 'Chốt' }, 2)).toBe('Chốt');
    expect(statusText({ 1: 'Nháp' }, 9)).toBe('9');
  });
});
