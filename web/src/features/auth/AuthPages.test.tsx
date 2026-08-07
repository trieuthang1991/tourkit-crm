import { render } from '@testing-library/react';
import type { ReactNode } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { MessageProvider } from '../../ui/message';
import { RegistrationPage } from '../registration/RegistrationPage';
import { LoginPage } from './LoginPage';

vi.mock('./AuthContext', () => ({
  useAuth: () => ({ login: vi.fn() }),
}));

vi.mock('../registration/registrationApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../registration/registrationApi')>();
  return {
    ...actual,
    useRegisterTenant: () => ({ mutateAsync: vi.fn() }),
  };
});

function renderPage(page: ReactNode) {
  return render(
    <MessageProvider>
      <MemoryRouter>{page}</MemoryRouter>
    </MessageProvider>,
  );
}

describe('auth page artwork', () => {
  it('renders distinct decorative images for login and registration', () => {
    const login = renderPage(<LoginPage />);
    const loginImage = login.container.querySelector<HTMLImageElement>('.mk-auth__visual');
    expect(loginImage).toBeInTheDocument();
    expect(loginImage?.getAttribute('src')).toContain('auth-login');
    login.unmount();

    const registration = renderPage(<RegistrationPage />);
    const registrationImage = registration.container.querySelector<HTMLImageElement>('.mk-auth__visual');
    expect(registrationImage).toBeInTheDocument();
    expect(registrationImage?.getAttribute('src')).toContain('auth-register');
  });
});
