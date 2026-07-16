// TourKit CRM — token cho Ant Design ConfigProvider.
// Thay thế object `theme` đang hard-code trong web/src/app/providers.tsx.
// Giữ nguyên logic providers; chỉ import và truyền `antdTheme` vào <ConfigProvider theme={antdTheme}>.
import type { ThemeConfig } from 'antd';

// Giữ đồng bộ với styles/theme.css (các biến --tk-*).
const C = {
  accent: '#eb5324',
  canvas: '#f8f7fa',
  heading: '#5e5873',
  body: '#6e6b7b',
  line: '#f3f2f7',
  border: '#e6e3ee',
  sidebar: '#2f2f34',
  hover: '#faf9fc',
};

export const antdTheme: ThemeConfig = {
  token: {
    colorPrimary: C.accent,
    colorLink: C.accent,
    colorInfo: C.accent,
    borderRadius: 7,
    borderRadiusLG: 10,
    fontFamily: "'Geist Variable', 'Geist', -apple-system, 'Segoe UI', Arial, sans-serif",
    fontFamilyCode: "'JetBrains Mono Variable', 'JetBrains Mono', ui-monospace, monospace",
    colorBgLayout: C.canvas,
    colorText: C.body,
    colorTextHeading: C.heading,
    colorBorderSecondary: C.line,
    controlHeight: 40,
    boxShadow: '0 1px 2px rgba(34,41,47,.04), 0 4px 16px -6px rgba(34,41,47,.08)',
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
    Card: { borderRadiusLG: 16, boxShadowTertiary: '0 1px 2px rgba(34,41,47,.04), 0 8px 24px -10px rgba(34,41,47,.12)' },
    Table: {
      headerBg: C.line,
      headerColor: C.body,
      borderColor: C.line,
      rowHoverBg: C.hover,
      headerSplitColor: 'transparent',
      cellPaddingBlock: 13,
    },
    Button: { primaryShadow: '0 6px 16px -6px rgba(235,83,36,.4)', defaultBorderColor: C.border, fontWeight: 500 },
    Segmented: { itemSelectedBg: C.accent, itemSelectedColor: '#fff', trackBg: '#fff' },
    Modal: { borderRadiusLG: 14 },
    Input: { activeBorderColor: C.accent, hoverBorderColor: '#d3cfe0' },
    Select: { optionSelectedBg: 'rgba(235,83,36,.08)' },
    Tag: { borderRadiusSM: 20 },
  },
};
