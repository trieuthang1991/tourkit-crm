import { QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { queryClient } from './queryClient';
import { AuthProvider } from '../features/auth/AuthContext';
import { MessageProvider } from '../ui/message';

type AppProvidersProps = {
  children: ReactNode;
};

export function AppProviders({ children }: AppProvidersProps) {
  // Hệ Refined tự dựng — KHÔNG còn ConfigProvider/AntdApp của antd.
  // MessageProvider: toast Refined (thay App.useApp().message).
  return (
    <MessageProvider>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider>{children}</AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </MessageProvider>
  );
}
