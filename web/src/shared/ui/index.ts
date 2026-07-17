// Barrel export — feature import list-view từ đây thay vì 'antd' trực tiếp.
// LƯU Ý: Field ở đây là bản react-hook-form của repo (name-based, dùng trong CrudFormModal),
// KHÔNG phải bản presentational của handoff — giữ nguyên để không phá form hiện có.
export { PageHeader } from './PageHeader';
export { StatCard, StatRow } from './StatCard';
export { GradientStatCard, STAT_GRADIENTS } from './GradientStatCard';
export type { StatGradient } from './GradientStatCard';
export { WidgetCard, ListWidget } from './WidgetCard';
export { StatusTag, voucherTone, invoiceTone, activeTone } from './StatusTag';
export { CatalogStatusTag } from './CatalogStatusTag';
export { DataCard, FilterToolbar, InitialsAvatar } from './DataCard';
export { SegmentTabs } from './SegmentTabs';
export type { SegOption } from './SegmentTabs';
export { Button } from './Button';
export type { ButtonProps } from './Button';
export { CrudFormModal } from './CrudFormModal';
// Field (react-hook-form, name-based) — dùng bên trong CrudFormModal.
export { TextField, NumberField, TextAreaField, DatePickerField, SelectField, CheckboxField } from './Field';
