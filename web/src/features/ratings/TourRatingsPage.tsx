import type { ColumnsType } from 'antd/es/table';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { tourRatingsCrud } from './tourRatingsCrud';
import { tourRatingCreateSchema, tourRatingUpdateSchema } from './tourRatingTypes';
import type { TourRating, TourRatingForm } from './tourRatingTypes';

const columns: ColumnsType<TourRating> = [
  { title: 'Chuyến đi', dataIndex: 'tourDepartureId', key: 'tourDepartureId' },
  { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName' },
  { title: 'Số sao', dataIndex: 'stars', key: 'stars' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function TourRatingsPage() {
  return (
    <ResourcePage<TourRating, TourRatingForm>
      title="Đánh giá tour"
      columns={columns}
      crud={tourRatingsCrud}
      perms={{ create: 'rating.manage', update: 'rating.manage', remove: 'rating.manage' }}
      toForm={(r) => ({
        tourDepartureId: r?.tourDepartureId ?? null,
        orderId: r?.orderId ?? null,
        customerName: r?.customerName ?? null,
        customerPhone: r?.customerPhone ?? null,
        stars: r?.stars ?? 5,
        comment: r?.comment ?? null,
        status: r?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa đánh giá tour' : 'Thêm đánh giá tour'}
          schema={mode === 'edit' ? tourRatingUpdateSchema : tourRatingCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <TextField name="tourDepartureId" label="Mã chuyến đi" /> : null}
          {mode === 'create' ? <TextField name="orderId" label="Mã đơn hàng" /> : null}
          <TextField name="customerName" label="Tên khách hàng" />
          <TextField name="customerPhone" label="Điện thoại" />
          <NumberField name="stars" label="Số sao" required />
          <TextAreaField name="comment" label="Nhận xét" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
