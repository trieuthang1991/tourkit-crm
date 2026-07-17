import { App, Button, Card, Descriptions, InputNumber, Popconfirm, Select, Space, Typography } from '../../shared/ui/antd';
import { Controller, useForm } from 'react-hook-form';
import { Link, useParams } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { dateText } from '../../shared/format';
import { customersCrud } from '../customers/customersCrud';
import { useAuth } from '../auth/AuthContext';
import { useCreateBooking, useCreateHold } from './bookingApi';
import { useCloseDeparture, useDeparture } from './departuresApi';
import type { BookingRequestForm } from './seatTypes';

const DEFAULTS = { customerId: '', adultQty: 0, childQty: 0, childSmallQty: 0, babyQty: 0 } as unknown as BookingRequestForm;
const QTY_FIELDS: { name: keyof BookingRequestForm; label: string }[] = [
  { name: 'adultQty', label: 'Người lớn' },
  { name: 'childQty', label: 'Trẻ em' },
  { name: 'childSmallQty', label: 'Trẻ nhỏ' },
  { name: 'babyQty', label: 'Em bé' },
];

export function DepartureDetailPage() {
  const { id } = useParams<{ id: string }>();
  const departureId = id ?? '';
  const { message } = App.useApp();
  const { has } = useAuth();
  const { control, handleSubmit, reset, formState: { errors } } = useForm<BookingRequestForm>({ defaultValues: DEFAULTS });

  const departure = useDeparture(departureId);
  const customers = customersCrud.useList({ page: 1, size: 100 });
  const createBooking = useCreateBooking(departureId);
  const createHold = useCreateHold(departureId);
  const closeDeparture = useCloseDeparture();

  async function handleClose() {
    try {
      await closeDeparture.mutateAsync(departureId);
      message.success('Đã đóng chuyến');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const customerOptions = (customers.data?.items ?? []).map((c) => ({ value: c.id, label: c.fullName }));

  function submit(action: 'book' | 'hold') {
    return handleSubmit(async (values) => {
      try {
        if (action === 'book') {
          const order = await createBooking.mutateAsync(values);
          message.success(
            <span>
              Đã chốt đơn <Link to={`/orders/${order.id}`}>{order.code}</Link>
            </span>,
          );
        } else {
          const seat = await createHold.mutateAsync(values);
          message.success(
            <span>
              Đã giữ chỗ (mã {seat.reservationCode ?? seat.id}) — <Link to={`/orders/${seat.orderId}`}>xem đơn hàng</Link>
            </span>,
          );
        }
        reset(DEFAULTS);
      } catch (e) {
        message.error(errorMessage(e));
      }
    });
  }

  return (
    <>
      <Typography.Title level={3}>Chuyến đi {departure.data?.code ?? ''}</Typography.Title>
      <Card
        loading={departure.isLoading}
        style={{ marginBottom: 16 }}
        extra={
          has('departure.close') ? (
            <Popconfirm title="Đóng chuyến này? Sau khi đóng sẽ không thể đặt/giữ chỗ thêm." okText="Đóng chuyến" onConfirm={handleClose}>
              <Button danger loading={closeDeparture.isPending}>
                Đóng chuyến
              </Button>
            </Popconfirm>
          ) : null
        }
      >
        <Descriptions column={2}>
          <Descriptions.Item label="Mã chuyến">{departure.data?.code}</Descriptions.Item>
          <Descriptions.Item label="Tên chuyến">{departure.data?.title}</Descriptions.Item>
          <Descriptions.Item label="Ngày khởi hành">{dateText(departure.data?.departureDate)}</Descriptions.Item>
          <Descriptions.Item label="Ngày kết thúc">{dateText(departure.data?.endDate)}</Descriptions.Item>
          <Descriptions.Item label="Tổng số chỗ">{departure.data?.totalSlots}</Descriptions.Item>
          <Descriptions.Item label="Trạng thái">{departure.data?.status}</Descriptions.Item>
        </Descriptions>
      </Card>
      {has('booking.create') ? (
        <Card title="Đặt chỗ">
          <div style={{ marginBottom: 16 }}>
            <label style={{ display: 'block', marginBottom: 6, fontSize: 12.5, fontWeight: 500, color: 'var(--tk-text-strong)' }}>
              Khách hàng <span style={{ color: 'var(--tk-danger)' }}>*</span>
            </label>
            <Controller
              name="customerId"
              control={control}
              rules={{ required: 'Bắt buộc' }}
              render={({ field }) => (
                <Select value={field.value} onChange={field.onChange} options={customerOptions} loading={customers.isLoading} showSearch placeholder="Chọn khách hàng" style={{ maxWidth: 360 }} />
              )}
            />
            {errors.customerId ? <div style={{ marginTop: 6, fontSize: 12, color: 'var(--tk-danger)' }}>{errors.customerId.message as string}</div> : null}
          </div>
          <Space wrap size="large">
            {QTY_FIELDS.map((f) => (
              <div key={f.name}>
                <label style={{ display: 'block', marginBottom: 6, fontSize: 12.5, fontWeight: 500, color: 'var(--tk-text-strong)' }}>{f.label}</label>
                <Controller
                  name={f.name}
                  control={control}
                  rules={{ required: 'Bắt buộc' }}
                  render={({ field }) => <InputNumber min={0} value={field.value as number} onChange={field.onChange} />}
                />
              </div>
            ))}
          </Space>
          <Space style={{ marginTop: 16 }}>
            <Button type="primary" loading={createBooking.isPending} onClick={submit('book')}>
              Chốt ngay
            </Button>
            <Button loading={createHold.isPending} onClick={submit('hold')}>
              Giữ chỗ
            </Button>
          </Space>
        </Card>
      ) : null}
    </>
  );
}
