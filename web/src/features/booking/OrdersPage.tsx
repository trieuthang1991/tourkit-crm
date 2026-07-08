import { Button } from 'antd';
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

  const columns: ColumnsType<Order> = [
    { title: 'Mã đơn', dataIndex: 'code', key: 'code' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: number) => statusText(ORDER_STATUS, status),
    },
    {
      title: 'Doanh thu',
      dataIndex: 'totalRevenue',
      key: 'totalRevenue',
      render: (v: number) => money(v),
    },
    {
      title: 'Chi phí',
      dataIndex: 'totalCost',
      key: 'totalCost',
      render: (v: number) => money(v),
    },
    {
      title: '',
      key: '__detail',
      width: 120,
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
