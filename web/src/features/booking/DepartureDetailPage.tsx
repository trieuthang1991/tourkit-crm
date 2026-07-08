import { App, Button, Card, Descriptions, Form, InputNumber, Select, Space, Typography } from 'antd';
import { Link, useParams } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { dateText } from '../../shared/format';
import { customersCrud } from '../customers/customersCrud';
import { useAuth } from '../auth/AuthContext';
import { useCreateBooking, useCreateHold } from './bookingApi';
import { useDeparture } from './departuresApi';
import type { BookingRequestForm } from './seatTypes';

export function DepartureDetailPage() {
  const { id } = useParams<{ id: string }>();
  const departureId = id ?? '';
  const { message } = App.useApp();
  const { has } = useAuth();
  const [form] = Form.useForm<BookingRequestForm>();

  const departure = useDeparture(departureId);
  const customers = customersCrud.useList({ page: 1, size: 100 });
  const createBooking = useCreateBooking(departureId);
  const createHold = useCreateHold(departureId);

  const customerOptions = (customers.data?.items ?? []).map((c) => ({ value: c.id, label: c.fullName }));

  async function submit(action: 'book' | 'hold') {
    try {
      const values = await form.validateFields();
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
            Đã giữ chỗ (mã {seat.reservationCode ?? seat.id}) —{' '}
            <Link to={`/orders/${seat.orderId}`}>xem đơn hàng</Link>
          </span>,
        );
      }
      form.resetFields();
    } catch (e) {
      // Lỗi validate của antd Form không có message dạng lỗi API — bỏ qua, antd tự hiển thị.
      if (e && typeof e === 'object' && 'errorFields' in e) {
        return;
      }
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <Typography.Title level={3}>Chuyến đi {departure.data?.code ?? ''}</Typography.Title>
      <Card loading={departure.isLoading} style={{ marginBottom: 16 }}>
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
          <Form
            form={form}
            layout="vertical"
            initialValues={{ adultQty: 0, childQty: 0, childSmallQty: 0, babyQty: 0 }}
          >
            <Form.Item name="customerId" label="Khách hàng" rules={[{ required: true, message: 'Bắt buộc' }]}>
              <Select
                options={customerOptions}
                loading={customers.isLoading}
                showSearch
                optionFilterProp="label"
                placeholder="Chọn khách hàng"
              />
            </Form.Item>
            <Space wrap size="large">
              <Form.Item name="adultQty" label="Người lớn" rules={[{ required: true, message: 'Bắt buộc' }]}>
                <InputNumber min={0} />
              </Form.Item>
              <Form.Item name="childQty" label="Trẻ em" rules={[{ required: true, message: 'Bắt buộc' }]}>
                <InputNumber min={0} />
              </Form.Item>
              <Form.Item name="childSmallQty" label="Trẻ nhỏ" rules={[{ required: true, message: 'Bắt buộc' }]}>
                <InputNumber min={0} />
              </Form.Item>
              <Form.Item name="babyQty" label="Em bé" rules={[{ required: true, message: 'Bắt buộc' }]}>
                <InputNumber min={0} />
              </Form.Item>
            </Space>
            <Space>
              <Button type="primary" loading={createBooking.isPending} onClick={() => submit('book')}>
                Chốt ngay
              </Button>
              <Button loading={createHold.isPending} onClick={() => submit('hold')}>
                Giữ chỗ
              </Button>
            </Space>
          </Form>
        </Card>
      ) : null}
    </>
  );
}
