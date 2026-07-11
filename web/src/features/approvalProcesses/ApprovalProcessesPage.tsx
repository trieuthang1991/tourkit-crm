import { App, Button, Card, Divider, Empty, Input, List, Modal, Popconfirm, Select, Space, Tag, Typography } from 'antd';
import { useMemo, useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import {
  useAddStep,
  useApprovalProcessDetail,
  useApprovalProcesses,
  useCreateProcess,
  useDeleteProcess,
  useDeleteStep,
  usePositionOptions,
  useSetStepUsers,
  useUserOptions,
} from './api';
import { APPROVAL_METHOD_OPTIONS, approvalMethodLabel } from './types';

function ProcessDetail({ processId, canManage }: { processId: string; canManage: boolean }) {
  const { message } = App.useApp();
  const detail = useApprovalProcessDetail(processId);
  const positions = usePositionOptions();
  const users = useUserOptions();
  const addStep = useAddStep(processId);
  const deleteStep = useDeleteStep(processId);
  const setStepUsers = useSetStepUsers(processId);
  const [newPositionId, setNewPositionId] = useState<string | undefined>();

  const positionOptions = useMemo(
    () => (positions.data ?? []).map((p) => ({ label: p.name, value: p.id })),
    [positions.data],
  );
  const userOptions = useMemo(
    () => (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id })),
    [users.data],
  );

  async function handleAddStep() {
    if (!newPositionId) return;
    try {
      await addStep.mutateAsync(newPositionId);
      setNewPositionId(undefined);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  if (!detail.data) {
    return <Card loading />;
  }

  return (
    <Card
      title={
        <Space>
          <span>{detail.data.name}</span>
          <Tag color="blue">{approvalMethodLabel(detail.data.method)}</Tag>
        </Space>
      }
    >
      <List
        dataSource={detail.data.steps}
        locale={{ emptyText: 'Chưa có bước duyệt' }}
        renderItem={(step) => (
          <List.Item
            actions={
              canManage
                ? [
                    <Popconfirm key="del" title="Xoá bước này?" onConfirm={() => deleteStep.mutate(step.id)}>
                      <Button size="small" danger>
                        Xoá bước
                      </Button>
                    </Popconfirm>,
                  ]
                : []
            }
          >
            <List.Item.Meta
              title={
                <Space>
                  <Tag>Bước {step.stepOrder}</Tag>
                  <span>{step.positionName ?? '(chức vụ đã xoá)'}</span>
                </Space>
              }
              description={
                canManage ? (
                  <Select
                    mode="multiple"
                    style={{ width: '100%', maxWidth: 480 }}
                    placeholder="Người duyệt ở bước này"
                    options={userOptions}
                    value={step.userIds}
                    onChange={(userIds) => setStepUsers.mutate({ stepId: step.id, userIds })}
                  />
                ) : step.userNames.length ? (
                  step.userNames.map((n) => <Tag key={n}>{n}</Tag>)
                ) : (
                  <Typography.Text type="secondary">Chưa gán người duyệt</Typography.Text>
                )
              }
            />
          </List.Item>
        )}
      />
      {canManage ? (
        <>
          <Divider />
          <Space>
            <Select
              style={{ width: 260 }}
              placeholder="Chọn chức vụ cho bước mới"
              options={positionOptions}
              value={newPositionId}
              onChange={setNewPositionId}
              showSearch
              optionFilterProp="label"
            />
            <Button type="primary" loading={addStep.isPending} disabled={!newPositionId} onClick={handleAddStep}>
              Thêm bước
            </Button>
          </Space>
        </>
      ) : null}
    </Card>
  );
}

export function ApprovalProcessesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('approvalprocess.manage');
  const list = useApprovalProcesses();
  const create = useCreateProcess();
  const remove = useDeleteProcess();
  const [selected, setSelected] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [method, setMethod] = useState(1);

  async function submit() {
    try {
      await create.mutateAsync({ name, method });
      message.success('Đã tạo quy trình');
      setName('');
      setMethod(1);
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <PageHeader
        title="Quy trình duyệt"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => setOpen(true)}>
              Thêm quy trình
            </Button>
          ) : null
        }
      />
      <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start' }}>
        <Card style={{ width: 320, flex: '0 0 auto' }} styles={{ body: { padding: 0 } }}>
          <List
            dataSource={list.data ?? []}
            loading={list.isLoading}
            locale={{ emptyText: 'Chưa có quy trình' }}
            renderItem={(p) => (
              <List.Item
                style={{ padding: '8px 16px', cursor: 'pointer', background: selected === p.id ? '#e6f4ff' : undefined }}
                onClick={() => setSelected(p.id)}
                actions={
                  canManage
                    ? [
                        <Popconfirm key="del" title="Xoá quy trình này?" onConfirm={() => {
                          remove.mutate(p.id);
                          if (selected === p.id) setSelected(null);
                        }}>
                          <Button size="small" type="text" danger>
                            ✕
                          </Button>
                        </Popconfirm>,
                      ]
                    : []
                }
              >
                <List.Item.Meta
                  title={p.name}
                  description={<Tag>{p.stepCount} bước</Tag>}
                />
              </List.Item>
            )}
          />
        </Card>
        <div style={{ flex: 1 }}>
          {selected ? (
            <ProcessDetail processId={selected} canManage={canManage} />
          ) : (
            <Empty description="Chọn một quy trình để xem/sửa bước duyệt" />
          )}
        </div>
      </div>
      <Modal
        open={open}
        title="Thêm quy trình duyệt"
        onCancel={() => setOpen(false)}
        onOk={submit}
        confirmLoading={create.isPending}
        okButtonProps={{ disabled: !name.trim() }}
        destroyOnHidden
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <Input placeholder="Tên quy trình (vd: Duyệt chi trên 10 triệu)" value={name} onChange={(e) => setName(e.target.value)} />
          <Select style={{ width: '100%' }} options={APPROVAL_METHOD_OPTIONS} value={method} onChange={setMethod} />
        </Space>
      </Modal>
    </>
  );
}
