import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import 'antd/dist/reset.css';
import '@fontsource-variable/geist'; // sans chính (heading + body) — thay Roboto
import '@fontsource-variable/jetbrains-mono'; // mono cho số/tiền/mã — thay Roboto Mono
import './styles/theme.css'; // biến --tk-* + reset + nền app (bộ handoff CSS)
import './styles/components.css'; // class .tk-* cho shell/card/table/pill/form
import './styles/tailwind.css'; // Tailwind utilities (no preflight) — cho component AntD-free (pilot)
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
