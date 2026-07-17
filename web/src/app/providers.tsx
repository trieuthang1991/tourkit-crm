import { App as AntdApp, ConfigProvider } from 'antd';
import { QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { queryClient } from './queryClient';
import { antdTheme } from './theme';
import { AuthProvider } from '../features/auth/AuthContext';
import { MessageProvider } from '../ui/message';

type AppProvidersProps = {
  children: ReactNode;
};

export function AppProviders({ children }: AppProvidersProps) {
  return (
    // ConfigProvider + AntdApp: TẠM THỜI cho các màn CHƯA migrate sang hệ Refined.
    // MessageProvider: toast của hệ Refined (thay App.useApp().message). Gỡ AntD khi migrate xong.
    <ConfigProvider theme={antdTheme}>
      <AntdApp>
        <MessageProvider>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <AuthProvider>{children}</AuthProvider>
            </BrowserRouter>
          </QueryClientProvider>
        </MessageProvider>
      </AntdApp>
    </ConfigProvider>
  );
}
