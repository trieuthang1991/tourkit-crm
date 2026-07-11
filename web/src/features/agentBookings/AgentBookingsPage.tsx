import { App, Button, Modal, Popconfirm, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { TextAreaField, TextField } from '../../shared/ui/Field';
import {
  useAddPassenger,
  useAgentBooking,
  useAgentBookings,
  useCreateAgentBooking,
  useRemovePassenger,
} from './agentBookingsApi';
import { AGENT_BOOKING_STATUS, addPassengerFormSchema, createAgentBookingFormSchema } from './types';
import type { AddPassengerForm, AgentBookingSummary, AgentPassenger, CreateAgentBookingForm } from './types';

export function AgentBookingsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('agentquote.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const list = useAgentBookings(page, size);

  const [creating, setCreating] = useState(false);
  const [paxBookingId, setPaxBookingId] = useState<string | null>(null);
  const [addingPax, setAddingPax] = useState(false);

  const create = useCreateAgentBooking();
  const addPax = useAddPassenger();
  const removePax = useRemovePassenger();
  const detail = useAgentBooking(paxBookingId ?? '');

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onCreate(values: CreateAgentBookingForm) {
    await run(async () => {
      await create.mutateAsync(values);
      setCreating(false);
    }, 'Đã tạo booking');
  }

  async function onAddPax(values: AddPassengerForm) {
    if (!paxBookingId) return;
    await run(async () => {
      await addPax.mutateAsync({ bookingId: paxBookingId, body: values });
      setAddingPax(false);
    }, 'Đã thêm hành khách');
  }

  const columns: ColumnsType<AgentBookingSummary> = [
    { title: 'Mã', dataIndex: 'code', key: 'code' },
    { title: 'Đại lý', dataIndex: 'agentId', key: 'agentId' },
    { title: 'Tổng tiền', dataIndex: 'totalAmount', key: 'totalAmount', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => statusText(AGENT_BOOKING_STATUS, v) },
    {
      title: '',
      key: '__actions',
      width: 140,
      render: (_: unknown, item: AgentBookingSummary) => (
        <Button size="small" onClick={() => setPaxBookingId(item.id)}>
          Hành khách
        </Button>
      ),
    },
  ];

  const paxColumns: ColumnsType<AgentPassenger> = [
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    { title: 'Hộ chiếu', dataIndex: 'passportNo', key: 'passportNo' },
    { title: 'Quốc tịch', dataIndex: 'nationality', key: 'nationality' },
    {
      title: '',
      key: '__x',
      width: 80,
      render: (_: unknown, p: AgentPassenger) =>
        canManage ? (
          <Popconfirm
            title="Xoá hành khách?"
            onConfirm={() => run(() => removePax.mutateAsync({ bookingId: paxBookingId!, passengerId: p.id }), 'Đã xoá')}
          >
            <Button size="small" danger>
              Xoá
            </Button>
          </Popconfirm>
        ) : null,
    },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Đặt chỗ Đại lý (B2B)
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setCreating(true)}>
            Tạo từ báo giá
          </Button>
        ) : null}
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
      />

      {creating ? (
        <CrudFormModal
          open
          title="Tạo booking từ báo giá đã xác nhận"
          schema={createAgentBookingFormSchema}
          defaultValues={{ quoteRequestId: '', code: '', note: null }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={onCreate}
        >
          <TextField name="quoteRequestId" label="Mã yêu cầu báo giá (Confirmed)" required />
          <TextField name="code" label="Mã booking" />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}

      <Modal
        open={!!paxBookingId}
        title="Hành khách"
        footer={null}
        onCancel={() => setPaxBookingId(null)}
        width={640}
      >
        {canManage ? (
          <Button style={{ marginBottom: 12 }} onClick={() => setAddingPax(true)}>
            + Thêm hành khách
          </Button>
        ) : null}
        <Table
          rowKey="id"
          size="small"
          columns={paxColumns}
          dataSource={detail.data?.passengers ?? []}
          loading={detail.isLoading}
          pagination={false}
        />
      </Modal>

      {addingPax ? (
        <CrudFormModal
          open
          title="Thêm hành khách"
          schema={addPassengerFormSchema}
          defaultValues={{ fullName: '', dateOfBirth: null, passportNo: null, nationality: null, note: null }}
          submitting={addPax.isPending}
          onCancel={() => setAddingPax(false)}
          onSubmit={onAddPax}
        >
          <TextField name="fullName" label="Họ tên" required />
          <TextField name="passportNo" label="Số hộ chiếu" />
          <TextField name="nationality" label="Quốc tịch" />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}
    </>
  );
}
