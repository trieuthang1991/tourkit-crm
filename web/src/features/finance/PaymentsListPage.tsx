import { App, Button, Popconfirm, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { paymentListItemSchema, VOUCHER_STATUS, voucherStatusColor } from './listTypes';
import type { PaymentListItem } from './listTypes';

const KEY = ['payments-all'];

export function PaymentsListPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canApprove = has('payment.approve');
  const qc = useQueryClient();
  const [page, setPage] = useState(DEFAULT_PAGE);

  const list = useQuery({
    queryKey: [...KEY, page.page, page.size],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payments', { params: page });
      return pagedSchema(paymentListItemSchema).parse(data);
    },
  });

  const act = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'approve' | 'reject' }) => {
      await httpClient.post(`/api/v1/payments/${id}/${action}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });

  async function run(id: string, action: 'approve' | 'reject') {
    try {
      await act.mutateAsync({ id, action });
      message.success(action === 'approve' ? 'Đã duyệt' : 'Đã từ chối');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const dateVi = (v: string) => new Date(v).toLocaleDateString('vi-VN');

  const columns: ColumnsType<PaymentListItem> = [
    { title: 'Mã phiếu', dataIndex: 'code', key: 'code', fixed: 'left', width: 140 },
    { title: 'Mã đơn', dataIndex: 'orderCode', key: 'orderCode', width: 130, render: (v: string | null) => v ?? '—' },
    { title: 'Nhà cung cấp', dataIndex: 'providerName', key: 'providerName', width: 180, render: (v: string | null) => v ?? '—' },
    { title: 'Số tiền', dataIndex: 'amount', key: 'amount', width: 140, align: 'right', render: (v: number) => money(v) },
    { title: 'Hình thức', dataIndex: 'paymentMethod', key: 'paymentMethod', width: 110 },
    { title: 'Ngày', dataIndex: 'issuedAt', key: 'issuedAt', width: 110, render: dateVi },
    { title: 'Người nhận', dataIndex: 'receiverName', key: 'receiverName', width: 150, render: (v: string | null) => v ?? '—' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (s: number) => <Tag color={voucherStatusColor(s)}>{VOUCHER_STATUS[s] ?? s}</Tag>,
    },
    ...(canApprove
      ? [
          {
            title: '',
            key: '__actions',
            width: 170,
            fixed: 'right' as const,
            render: (_: unknown, r: PaymentListItem) =>
              r.status === 0 ? (
                <Space>
                  <Popconfirm title="Duyệt phiếu chi này?" onConfirm={() => run(r.id, 'approve')}>
                    <Button size="small" type="primary">
                      Duyệt
                    </Button>
                  </Popconfirm>
                  <Popconfirm title="Từ chối phiếu này?" onConfirm={() => run(r.id, 'reject')}>
                    <Button size="small" danger>
                      Từ chối
                    </Button>
                  </Popconfirm>
                </Space>
              ) : null,
          } as ColumnsType<PaymentListItem>[number],
        ]
      : []),
  ];

  return (
    <>
      <PageHeader title="Phiếu chi" />
      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{
          current: page.page,
          pageSize: page.size,
          total: list.data?.total ?? 0,
          onChange: (p, s) => setPage({ page: p, size: s }),
        }}
      />
    </>
  );
}
