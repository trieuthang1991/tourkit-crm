import { Button } from '../../ui/kit';

/* Nút "Xuất Excel" dùng chung — ghost + icon tải xuống, theo style .rf-btn của app.
   onExport do trang cung cấp: tự dựng headers + rows rồi gọi exportRowsToCsv. */
export function ExportButton({
  filename,
  onExport,
  label = 'Xuất Excel',
  title = 'Xuất trang hiện tại ra Excel/CSV',
  disabled,
}: {
  filename: string;
  onExport: () => void;
  label?: string;
  title?: string;
  disabled?: boolean;
}) {
  return (
    <span title={title} aria-label={`Xuất ${filename}`}>
      <Button variant="ghost" icon="download" onClick={onExport} disabled={disabled}>
        {label}
      </Button>
    </span>
  );
}
