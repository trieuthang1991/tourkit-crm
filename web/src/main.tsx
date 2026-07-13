import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import 'antd/dist/reset.css';
import './styles/vuexy.scss'; // Bootstrap SCOPED trong .vuexy-scope — không đè AntD (xem docs/vuexy-migration.md)
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
