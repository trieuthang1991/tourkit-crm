import { App, Button, Input, Modal, Popconfirm, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { useCreateWorkflow, useDeleteWorkflow, useWorkflows } from './api';
import type { Workflow } from './types';

export function WorkflowsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const navigate = useNavigate();
  const canManage = has('workflow.manage');
  const list = useWorkflows();
  const create = useCreateWorkflow();
  const remove = useDeleteWorkflow();
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');

  async function submit() {
    try {
      await create.mutateAsync({ name, startDate: null, endDate: null });
      message.success('Đã tạo board');
      setName('');
      setOpen(false);
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

  const columns: ColumnsType<Workflow> = [
    { title: 'Tên board', dataIndex: 'name', key: 'name' },
    { title: 'Số cột', dataIndex: 'sectionCount', key: 'sectionCount', width: 100 },
    { title: 'Số việc', dataIndex: 'taskCount', key: 'taskCount', width: 100 },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (v: number) => <Tag color={v === 0 ? 'blue' : 'default'}>{v === 0 ? 'Đang dùng' : 'Lưu trữ'}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 180,
      render: (_: unknown, item: Workflow) => (
        <Space>
          <Button size="small" type="link" onClick={() => navigate(`/workflows/${item.id}`)}>
            Mở board
          </Button>
          {canManage ? (
            <Popconfirm title="Xoá board này? (việc sẽ được tách khỏi board)" onConfirm={() => handleDelete(item.id)}>
              <Button size="small" danger loading={remove.isPending}>
                Xoá
              </Button>
            </Popconfirm>
          ) : null}
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Quy trình / Board Kanban"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => setOpen(true)}>
              Thêm board
            </Button>
          ) : null
        }
      />
      <Table rowKey="id" columns={columns} dataSource={list.data ?? []} loading={list.isLoading} pagination={false} />
      <Modal
        open={open}
        title="Thêm board"
        onCancel={() => setOpen(false)}
        onOk={submit}
        confirmLoading={create.isPending}
        okButtonProps={{ disabled: !name.trim() }}
        destroyOnHidden
      >
        <Input placeholder="Tên board (vd: Điều hành tour hè)" value={name} onChange={(e) => setName(e.target.value)} />
      </Modal>
    </>
  );
}
