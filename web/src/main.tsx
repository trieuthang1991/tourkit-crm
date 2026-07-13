import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import 'antd/dist/reset.css';
import './styles/theme-vuexy.css'; // chỉnh Ant Design ra chất admin hiện đại (Vuexy-like)
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
