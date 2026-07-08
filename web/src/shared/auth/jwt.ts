export type TokenClaims = {
  permissions: string[];
  email: string | null;
  tenantId: string | null;
};

function base64UrlDecode(input: string): string {
  const padded = input.replace(/-/g, '+').replace(/_/g, '/');
  return decodeURIComponent(
    atob(padded)
      .split('')
      .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  );
}

export function decodeToken(token: string | null): TokenClaims | null {
  if (!token) {
    return null;
  }
  const parts = token.split('.');
  const payloadPart = parts[1];
  if (parts.length < 2 || !payloadPart) {
    return null;
  }
  try {
    const payload = JSON.parse(base64UrlDecode(payloadPart)) as Record<string, unknown>;
    const rawPerm = payload.perm;
    const permissions = Array.isArray(rawPerm)
      ? (rawPerm as string[])
      : typeof rawPerm === 'string'
        ? [rawPerm]
        : [];
    return {
      permissions,
      email: typeof payload.email === 'string' ? payload.email : null,
      tenantId: typeof payload.tenant_id === 'string' ? payload.tenant_id : null,
    };
  } catch {
    return null;
  }
}
