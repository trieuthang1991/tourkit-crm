import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import 'antd/dist/reset.css';
import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import '@fontsource/roboto-mono/400.css';
import '@fontsource/roboto-mono/600.css';
import './styles/theme.css'; // biến --tk-* + reset + nền app (bộ handoff CSS)
import './styles/components.css'; // class .tk-* cho shell/card/table/pill/form
import { AppProviders } from './app/providers';
import { AppRouter } from './app/router';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Không tìm thấy #root');
}

createRoot(rootElement).render(
  <StrictMode>
    <AppProviders>
      <AppRouter />
    </AppProviders>
  </StrictMode>,
);
