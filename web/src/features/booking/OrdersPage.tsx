import { Button, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import { money, statusText } from '../../shared/format';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { ordersCrud } from './bookingApi';
import { ORDER_STATUS } from './seatTypes';
import type { Order } from './seatTypes';

// GET /api/v1/orders không có POST/PUT/DELETE ở màn danh sách này — chỉ dùng useList + getId,
// ResourcePage tự ẩn nút Thêm mới/Sửa/Xoá khi thiếu useCreate/useUpdate/useRemove.
const listOnlyCrud = { useList: ordersCrud.useList, getId: ordersCrud.getId };

export function OrdersPage() {
  const navigate = useNavigate();

  const dateVi = (v?: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');

  // Cột bám danh sách đơn hệ cũ: mã/khách/tour/ngày đi + doanh thu/đã thu/còn nợ/trạng thái.
  const columns: ColumnsType<Order> = [
    { title: 'Mã đơn', dataIndex: 'code', key: 'code', fixed: 'left', width: 130 },
    { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName', width: 170, render: (v?: string | null) => v ?? '—' },
    { title: 'Tour', dataIndex: 'tourTitle', key: 'tourTitle', width: 200, ellipsis: true, render: (v?: string | null) => v ?? '—' },
    { title: 'Ngày đi', dataIndex: 'departureDate', key: 'departureDate', width: 110, render: dateVi },
    { title: 'Doanh thu', dataIndex: 'totalRevenue', key: 'totalRevenue', width: 130, align: 'right', render: (v: number) => money(v) },
    { title: 'Đã thu', dataIndex: 'amountPaid', key: 'amountPaid', width: 130, align: 'right', render: (v?: number) => money(v ?? 0) },
    {
      title: 'Còn nợ',
      dataIndex: 'outstanding',
      key: 'outstanding',
      width: 130,
      align: 'right',
      render: (v?: number) => <span style={{ color: (v ?? 0) > 0 ? '#cf1322' : undefined }}>{money(v ?? 0)}</span>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 110,
      render: (status: number) => {
        const color = status === 2 ? 'green' : status === 3 ? 'red' : 'default';
        return <Tag color={color}>{statusText(ORDER_STATUS, status)}</Tag>;
      },
    },
    {
      title: '',
      key: '__detail',
      width: 100,
      fixed: 'right',
      render: (_: unknown, item: Order) => (
        <Button size="small" onClick={() => navigate(`/orders/${item.id}`, { state: { order: item } })}>
          Chi tiết
        </Button>
      ),
    },
  ];

  return (
    <ResourcePage<Order, object>
      title="Đơn hàng"
      columns={columns}
      crud={listOnlyCrud}
      perms={{}}
      toForm={() => ({})}
      renderForm={() => null}
      formModal={() => null}
    />
  );
}
