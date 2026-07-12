import { App as AntdApp, ConfigProvider } from 'antd';
import { QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { queryClient } from './queryClient';
import { AuthProvider } from '../features/auth/AuthContext';

type AppProvidersProps = {
  children: ReactNode;
};

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <ConfigProvider
      theme={{
        token: {
          // Brand TourKit (bám staging hệ cũ): đỏ-cam #EB5324, font Roboto.
          colorPrimary: '#EB5324',
          colorLink: '#EB5324',
          colorInfo: '#EB5324',
          borderRadius: 8,
          fontFamily: "'Roboto', -apple-system, 'Segoe UI', Arial, sans-serif",
        },
        components: {
          Menu: {
            darkItemBg: '#333333',
            darkPopupBg: '#333333',
            darkSubMenuItemBg: '#2b2b2b',
            darkItemSelectedBg: '#EB5324',
            darkItemColor: 'rgba(255,255,255,0.75)',
            darkItemHoverColor: '#ffffff',
          },
        },
      }}
    >
      <AntdApp>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <AuthProvider>{children}</AuthProvider>
          </BrowserRouter>
        </QueryClientProvider>
      </AntdApp>
    </ConfigProvider>
  );
}
