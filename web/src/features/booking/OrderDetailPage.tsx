import { App, Button, Card, Descriptions, Input, InputNumber, Modal, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { ordersCrud } from './bookingApi';
import { useCancelSeat, useConfirmSeat, useDepositSeat, useOrderLines } from './orderLinesApi';
import type { BookingLine } from './orderLinesApi';
import { ORDER_STATUS } from './seatTypes';
import type { Order } from './seatTypes';

const { TextArea } = Input;

function DepositModal({
  seatId,
  orderId,
  open,
  onClose,
}: {
  seatId: string;
  orderId: string;
  open: boolean;
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const [amount, setAmount] = useState<number>(0);
  const deposit = useDepositSeat(orderId);

  return (
    <Modal
      open={open}
      title="Đặt cọc"
      onCancel={onClose}
      confirmLoading={deposit.isPending}
      destroyOnClose
      onOk={async () => {
        try {
          await deposit.mutateAsync({ seatId, amount });
          message.success('Đã đặt cọc');
          setAmount(0);
          onClose();
        } catch (e) {
          message.error(errorMessage(e));
        }
      }}
    >
      <InputNumber
        style={{ width: '100%' }}
        min={0}
        value={amount}
        onChange={(v) => setAmount(v ?? 0)}
        placeholder="Số tiền cọc"
      />
    </Modal>
  );
}

function CancelModal({
  seatId,
  orderId,
  open,
  onClose,
}: {
  seatId: string;
  orderId: string;
  open: boolean;
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const [note, setNote] = useState('');
  const [refundAmount, setRefundAmount] = useState<number>(0);
  const cancel = useCancelSeat(orderId);

  return (
    <Modal
      open={open}
      title="Huỷ chỗ"
      onCancel={onClose}
      confirmLoading={cancel.isPending}
      destroyOnClose
      onOk={async () => {
        try {
          await cancel.mutateAsync({ seatId, note: note ? note : null, refundAmount });
          message.success('Đã huỷ chỗ');
          setNote('');
          setRefundAmount(0);
          onClose();
        } catch (e) {
          message.error(errorMessage(e));
        }
      }}
    >
      <Space direction="vertical" style={{ width: '100%' }}>
        <TextArea rows={3} placeholder="Ghi chú" value={note} onChange={(e) => setNote(e.target.value)} />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={refundAmount}
          onChange={(v) => setRefundAmount(v ?? 0)}
          placeholder="Số tiền hoàn"
        />
      </Space>
    </Modal>
  );
}

function LineActions({ line, orderId }: { line: BookingLine; orderId: string }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const [depositOpen, setDepositOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);
  const confirmSeat = useConfirmSeat(orderId);

  return (
    <Space>
      {has('booking.seat.confirm') ? (
        <Button
          size="small"
          loading={confirmSeat.isPending}
          onClick={async () => {
            try {
              await confirmSeat.mutateAsync(line.id);
              message.success('Đã xác nhận chỗ');
            } catch (e) {
              message.error(errorMessage(e));
            }
          }}
        >
          Xác nhận
        </Button>
      ) : null}
      {has('booking.create') ? (
        <Button size="small" onClick={() => setDepositOpen(true)}>
          Đặt cọc
        </Button>
      ) : null}
      {has('booking.seat.cancel') ? (
        <Button size="small" danger onClick={() => setCancelOpen(true)}>
          Huỷ
        </Button>
      ) : null}
      <DepositModal seatId={line.id} orderId={orderId} open={depositOpen} onClose={() => setDepositOpen(false)} />
      <CancelModal seatId={line.id} orderId={orderId} open={cancelOpen} onClose={() => setCancelOpen(false)} />
    </Space>
  );
}

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const orderId = id ?? '';
  const location = useLocation();
  const stateOrder = (location.state as { order?: Order } | null)?.order;

  // Backend không có GET /api/v1/orders/{id} đơn lẻ — header dùng Order truyền qua navigation
  // state từ OrdersPage (điều hướng bình thường). Nếu vào thẳng URL (F5, mở link), fallback lấy
  // trang danh sách lớn (size=200, giới hạn tối đa của API) rồi tìm theo id.
  const ordersFallback = ordersCrud.useList({ page: 1, size: 200 });
  const order = stateOrder ?? ordersFallback.data?.items.find((o) => o.id === orderId);

  const lines = useOrderLines(orderId);

  const columns: ColumnsType<BookingLine> = [
    { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity' },
    { title: 'Trẻ em', dataIndex: 'amountChildren', key: 'amountChildren' },
    { title: 'Trẻ nhỏ', dataIndex: 'amountChildrenSmall', key: 'amountChildrenSmall' },
    { title: 'Em bé', dataIndex: 'quantityBaby', key: 'quantityBaby' },
    { title: 'Giá NL', dataIndex: 'priceAdult', key: 'priceAdult', render: (v: number) => money(v) },
    { title: 'Đã thu', dataIndex: 'upfrontAmount', key: 'upfrontAmount', render: (v: number) => money(v) },
    { title: 'Mã giữ chỗ', dataIndex: 'reservationCode', key: 'reservationCode' },
    {
      title: 'Liên hệ chính',
      dataIndex: 'isMainContact',
      key: 'isMainContact',
      render: (v: boolean) => (v ? 'Có' : ''),
    },
    {
      title: '',
      key: '__lineActions',
      render: (_: unknown, line: BookingLine) => <LineActions line={line} orderId={orderId} />,
    },
  ];

  return (
    <>
      <Typography.Title level={3}>Đơn hàng {order?.code ?? ''}</Typography.Title>
      <Card loading={!order} style={{ marginBottom: 16 }}>
        <Descriptions column={2}>
          <Descriptions.Item label="Mã đơn">{order?.code}</Descriptions.Item>
          <Descriptions.Item label="Trạng thái">
            {order ? statusText(ORDER_STATUS, order.status) : ''}
          </Descriptions.Item>
          <Descriptions.Item label="Doanh thu">{order ? money(order.totalRevenue) : ''}</Descriptions.Item>
          <Descriptions.Item label="Chi phí">{order ? money(order.totalCost) : ''}</Descriptions.Item>
        </Descriptions>
      </Card>
      <Card title="Dòng khách">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={lines.data ?? []}
          loading={lines.isLoading}
          pagination={false}
        />
      </Card>
      {/* Finance/Commission panels mounted in later phases */}
    </>
  );
}
