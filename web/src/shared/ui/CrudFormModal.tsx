// Panel form CRUD trượt phải. ĐÃ MIGRATE sang hệ Refined (Drawer tự dựng, KHÔNG antd).
// CrudDrawer trong ui/form có API y hệt (open/title/schema/defaultValues/submitting/
// onCancel/onSubmit/width/children) → re-export để 43 màn không phải sửa.
export { CrudDrawer as CrudFormModal } from '../../ui/form';
