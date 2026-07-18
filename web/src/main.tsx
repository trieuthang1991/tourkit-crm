import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@fontsource-variable/public-sans'; // sans chính hệ Vuexy (heading + body)
import '@fontsource-variable/geist'; // (dự phòng) sans hệ Refined cũ
import '@fontsource-variable/jetbrains-mono'; // mono cho số/tiền/mã
import 'material-symbols/outlined.css'; // icon hệ Refined — thay @ant-design/icons
import './styles/theme.css'; // biến --tk-* + reset + nền app (bộ handoff CSS)
import './styles/components.css'; // class .tk-* — lớp CŨ, cho màn chưa migrate (sẽ gỡ)
import './styles/refined.css'; // class .rf-* — hệ Refined (design handoff)
import './styles/marketing.css'; // class .mk-* — trang công khai (đăng nhập + landing)
import './styles/tailwind.css'; // Tailwind utilities (no preflight) — tiện ích layout
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
