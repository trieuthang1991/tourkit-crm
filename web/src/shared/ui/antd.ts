// Re-export AntD nguyên bản để feature dùng qua '@/shared/ui/antd' thay vì import 'antd' trực tiếp.
// ESLint chặn import 'antd' trong src/features/** (xem .eslintrc.cjs) → lõi AntD gom về 1 chỗ,
// sau này muốn đổi/bỏ AntD chỉ sửa ở đây. Component/API giữ NGUYÊN (không đổi hành vi).
export * from 'antd';
export type { ColumnsType } from 'antd/es/table';
