// TourKit CRM — token cho Ant Design ConfigProvider.
// TẠM THỜI: chỉ phục vụ các màn CHƯA migrate sang hệ "Refined". Nguồn chuẩn của token là
// styles/theme.css (:root --tk-*). XOÁ file này + <ConfigProvider> khi gỡ hẳn antd.
import type { ThemeConfig } from 'antd';

// Bảng màu Refined (đồng bộ styles/theme.css) để màn cũ không lệch tông trong lúc chuyển tiếp.
const C = {
  accent: '#eb5324',
  canvas: '#f6f6f7',
  heading: '#17181c',
  body: '#56575f',
  line: '#f2f2f4',
  border: '#e2e2e5',
  sidebar: '#2f2f34', // AppShell cũ — bước 3 thay bằng rail sáng
  hover: '#fafafa',
  headerBg: '#fafafa',
};

export const antdTheme: ThemeConfig = {
  token: {
    colorPrimary: C.accent,
    colorLink: C.accent,
    colorInfo: C.accent,
    borderRadius: 8,
    borderRadiusLG: 10,
    fontFamily: "'Geist Variable', 'Geist', -apple-system, 'Segoe UI', Arial, sans-serif",
    fontFamilyCode: "'JetBrains Mono Variable', 'JetBrains Mono', ui-monospace, monospace",
    colorBgLayout: C.canvas,
    colorText: C.body,
    colorTextHeading: C.heading,
    colorBorderSecondary: C.line,
    controlHeight: 38,
    boxShadow: '0 1px 2px rgba(20,20,40,.04)',
  },
  components: {
    Menu: {
      darkItemBg: C.sidebar,
      darkPopupBg: C.sidebar,
      darkSubMenuItemBg: '#282a2e',
      darkItemSelectedBg: C.accent,
      darkItemColor: 'rgba(255,255,255,0.72)',
      darkItemHoverColor: '#ffffff',
      itemBorderRadius: 7,
      itemMarginInline: 8,
    },
    Card: { borderRadiusLG: 12, boxShadowTertiary: '0 1px 2px rgba(20,20,40,.04)' },
    Table: {
      headerBg: C.headerBg,
      headerColor: '#a9aab0',
      borderColor: C.line,
      rowHoverBg: C.hover,
      headerSplitColor: 'transparent',
      cellPaddingBlock: 11,
    },
    Button: { primaryShadow: '0 6px 16px -6px rgba(235,83,36,.5)', defaultBorderColor: C.border, fontWeight: 500 },
    Segmented: { itemSelectedBg: C.accent, itemSelectedColor: '#fff', trackBg: '#fff' },
    Modal: { borderRadiusLG: 14 },
    Input: { activeBorderColor: C.accent, hoverBorderColor: '#d3cfe0' },
    Select: { optionSelectedBg: 'rgba(235,83,36,.08)' },
    Tag: { borderRadiusSM: 20 },
  },
};
