import { App, Button, Popconfirm, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { transferReasonSchema } from './transferApi';
import type { TransferReason } from './transferApi';

const QUERY_KEY = ['transfer-reasons'];

const formSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
type FormValues = z.infer<typeof formSchema>;

function useReasons() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/transfer-reasons');
      return z.array(transferReasonSchema).parse(data);
    },
  });
}

export function TransferReasonsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const qc = useQueryClient();
  const canManage = has('booking.create');
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<TransferReason | null>(null);
  const list = useReasons();

  const invalidate = () => qc.invalidateQueries({ queryKey: QUERY_KEY });

  const create = useMutation({
    mutationFn: async (body: FormValues) => {
      await httpClient.post('/api/v1/transfer-reasons', body);
    },
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: async ({ id, body }: { id: string; body: FormValues }) => {
      await httpClient.put(`/api/v1/transfer-reasons/${id}`, body);
    },
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/transfer-reasons/${id}`);
    },
    onSuccess: invalidate,
  });

  async function submit(values: FormValues) {
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: values });
      } else {
        await create.mutateAsync(values);
      }
      message.success('Đã lưu');
      setOpen(false);
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function handleDelete(id: string) {
    try {
      await remove.mutateAsync(id);
      message.success('Đã xoá');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<TransferReason> = [
    { title: 'Lý do chuyển chuyến', dataIndex: 'name', key: 'name' },
    { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder', width: 100 },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 160,
            render: (_: unknown, item: TransferReason) => (
              <Space>
                <Button size="small" onClick={() => { setEditing(item); setOpen(true); }}>
                  Sửa
                </Button>
                <Popconfirm title="Xoá lý do này?" onConfirm={() => handleDelete(item.id)}>
                  <Button size="small" danger loading={remove.isPending}>
                    Xoá
                  </Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<TransferReason>[number],
        ]
      : []),
  ];

  return (
    <>
      <PageHeader
        title="Lý do chuyển chuyến"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => { setEditing(null); setOpen(true); }}>
              Thêm
            </Button>
          ) : null
        }
      />
      <Table rowKey="id" columns={columns} dataSource={list.data ?? []} loading={list.isLoading} pagination={false} />
      {open ? (
        <CrudFormModal
          open={open}
          title={editing ? 'Sửa lý do' : 'Thêm lý do'}
          schema={formSchema}
          defaultValues={editing ? { name: editing.name, sortOrder: editing.sortOrder } : { name: '', sortOrder: 0 }}
          submitting={create.isPending || update.isPending}
          onCancel={() => { setOpen(false); setEditing(null); }}
          onSubmit={submit}
        >
          <TextField name="name" label="Lý do" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
