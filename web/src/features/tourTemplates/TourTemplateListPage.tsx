import { Button } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { tourTemplatesCrud } from './tourTemplatesCrud';
import { tourTemplateCreateSchema, tourTemplateUpdateSchema } from './types';
import type { TourTemplate, TourTemplateForm } from './types';

export function TourTemplateListPage() {
  const navigate = useNavigate();

  const columns: ColumnsType<TourTemplate> = [
    { title: 'Mã tour', dataIndex: 'code', key: 'code' },
    { title: 'Tên tour', dataIndex: 'title', key: 'title' },
    {
      title: 'Giá người lớn',
      dataIndex: 'priceAdult',
      key: 'priceAdult',
      render: (value: number) => money(value),
    },
    { title: 'Tổng số chỗ', dataIndex: 'totalSlots', key: 'totalSlots' },
    {
      title: '',
      key: '__detail',
      width: 100,
      render: (_: unknown, item: TourTemplate) => (
        <Button size="small" onClick={() => navigate(`/tour-templates/${item.id}`)}>
          Chi tiết
        </Button>
      ),
    },
  ];

  return (
    <ResourcePage<TourTemplate, TourTemplateForm>
      title="Tour mẫu"
      columns={columns}
      crud={tourTemplatesCrud}
      perms={{ create: 'tour.create', update: 'tour.update', remove: 'tour.delete' }}
      toForm={(t) => ({
        code: t?.code ?? '',
        title: t?.title ?? '',
        tourType: t?.tourType ?? null,
        totalSlots: t?.totalSlots ?? 0,
        reservationHours: t?.reservationHours ?? 0,
        priceAdult: t?.priceAdult ?? 0,
        priceChild: t?.priceChild ?? 0,
        priceChildSmall: t?.priceChildSmall ?? 0,
        priceBaby: t?.priceBaby ?? 0,
        termsNote: t?.termsNote ?? null,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa tour mẫu' : 'Thêm tour mẫu'}
          schema={mode === 'edit' ? tourTemplateUpdateSchema : tourTemplateCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <TextField name="code" label="Mã tour" required /> : null}
          <TextField name="title" label="Tên tour" required />
          <TextField name="tourType" label="Loại tour" />
          <NumberField name="totalSlots" label="Tổng số chỗ" required />
          <NumberField name="reservationHours" label="Giờ giữ chỗ" required />
          <NumberField name="priceAdult" label="Giá người lớn" required />
          <NumberField name="priceChild" label="Giá trẻ em" required />
          <NumberField name="priceChildSmall" label="Giá trẻ nhỏ" required />
          <NumberField name="priceBaby" label="Giá em bé" required />
          <TextAreaField name="termsNote" label="Ghi chú điều khoản" />
        </CrudFormModal>
      )}
    />
  );
}
