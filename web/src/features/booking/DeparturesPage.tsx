import { Button } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import { dateText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { departuresCrud } from './departuresApi';
import { departureFormSchema } from './departureTypes';
import type { Departure, DepartureForm } from './departureTypes';

// Không có PUT/DELETE cho tour-departures — bỏ useUpdate/useRemove khỏi crud object truyền
// vào ResourcePage để ẩn nút Sửa/Xoá (chỉ còn Thêm mới + danh sách).
// eslint-disable-next-line @typescript-eslint/no-unused-vars -- useUpdate/useRemove intentionally discarded.
const { useUpdate: _omitUpdate, useRemove: _omitRemove, ...listCreateCrud } = departuresCrud;

export function DeparturesPage() {
  const navigate = useNavigate();

  const columns: ColumnsType<Departure> = [
    { title: 'Mã chuyến', dataIndex: 'code', key: 'code' },
    { title: 'Tên chuyến', dataIndex: 'title', key: 'title' },
    {
      title: 'Ngày khởi hành',
      dataIndex: 'departureDate',
      key: 'departureDate',
      render: (v: string | null) => dateText(v),
    },
    { title: 'Tổng số chỗ', dataIndex: 'totalSlots', key: 'totalSlots' },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
    {
      title: '',
      key: '__open',
      width: 120,
      render: (_: unknown, item: Departure) => (
        <Button size="small" onClick={() => navigate(`/departures/${item.id}`)}>
          Mở chuyến
        </Button>
      ),
    },
  ];

  return (
    <ResourcePage<Departure, DepartureForm>
      title="Chuyến đi"
      columns={columns}
      crud={listCreateCrud}
      perms={{ create: 'departure.create' }}
      toForm={() => ({
        templateId: null,
        code: '',
        title: '',
        departureDate: null,
        endDate: null,
        totalSlots: 0,
      })}
      renderForm={() => null}
      formModal={({ open, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title="Thêm chuyến đi"
          schema={departureFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="templateId" label="Tour mẫu (templateId)" />
          <TextField name="code" label="Mã chuyến" required />
          <TextField name="title" label="Tên chuyến" required />
          <DatePickerField name="departureDate" label="Ngày khởi hành" />
          <DatePickerField name="endDate" label="Ngày kết thúc" />
          <NumberField name="totalSlots" label="Tổng số chỗ" required />
        </CrudFormModal>
      )}
    />
  );
}
