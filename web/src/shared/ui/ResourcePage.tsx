import { App, Popconfirm, Space, Table } from './antd';
import type { ColumnsType } from './antd';
import { useState } from 'react';
import type { ReactNode } from 'react';
import { DEFAULT_PAGE } from '../api/paged';
import { errorMessage } from '../api/problem';
import { Button } from './Button';
import { DataCard } from './DataCard';
import { PageHeader } from './PageHeader';
import { useAuth } from '../../features/auth/AuthContext';

type ResourcePageProps<TItem, TForm> = {
  title: string;
  columns: ColumnsType<TItem>;
  crud: {
    useList: (p: { page: number; size: number }) => { data?: { items: TItem[]; total: number }; isLoading: boolean };
    useCreate?: () => { mutateAsync: (b: TForm) => Promise<unknown>; isPending: boolean };
    useUpdate?: () => { mutateAsync: (a: { id: string; body: TForm }) => Promise<unknown>; isPending: boolean };
    useRemove?: () => { mutateAsync: (id: string) => Promise<unknown>; isPending: boolean };
    getId: (i: TItem) => string;
  };
  perms: { create?: string; update?: string; remove?: string };
  toForm: (item: TItem | null) => TForm;
  renderForm: (mode: 'create' | 'edit') => ReactNode; // fields, inside CrudFormModal by caller
  formModal: (args: {
    open: boolean;
    mode: 'create' | 'edit';
    submitting: boolean;
    onCancel: () => void;
    onSubmit: (values: TForm) => void;
    defaultValues: TForm;
  }) => ReactNode;
};

export function ResourcePage<TItem, TForm>(props: ResourcePageProps<TItem, TForm>) {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [editing, setEditing] = useState<{ mode: 'create' | 'edit'; item: TItem | null } | null>(null);

  const list = props.crud.useList(page);
  const create = props.crud.useCreate?.();
  const update = props.crud.useUpdate?.();
  const remove = props.crud.useRemove?.();

  const canCreate = !!props.crud.useCreate && (!props.perms.create || has(props.perms.create));
  const canUpdate = !!props.crud.useUpdate && (!props.perms.update || has(props.perms.update));
  const canRemove = !!props.crud.useRemove && (!props.perms.remove || has(props.perms.remove));

  const columns: ColumnsType<TItem> = [
    ...props.columns,
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: TItem) => (
        <Space>
          {canUpdate ? (
            <Button variant="ghost" size="small" onClick={() => setEditing({ mode: 'edit', item })}>
              Sửa
            </Button>
          ) : null}
          {canRemove ? (
            <Popconfirm
              title="Xoá bản ghi này?"
              onConfirm={async () => {
                try {
                  await remove!.mutateAsync(props.crud.getId(item));
                  message.success('Đã xoá');
                } catch (e) {
                  message.error(errorMessage(e));
                }
              }}
            >
              <Button variant="danger" size="small">
                Xoá
              </Button>
            </Popconfirm>
          ) : null}
        </Space>
      ),
    },
  ];

  async function submit(values: TForm) {
    try {
      if (editing?.mode === 'edit' && editing.item) {
        await update?.mutateAsync({ id: props.crud.getId(editing.item), body: values });
      } else {
        await create?.mutateAsync(values);
      }
      message.success('Đã lưu');
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <PageHeader
        title={props.title}
        extra={
          canCreate ? (
            <Button variant="primary" onClick={() => setEditing({ mode: 'create', item: null })}>
              Thêm mới
            </Button>
          ) : null
        }
      />
      <DataCard>
      <Table
        rowKey={(i) => props.crud.getId(i)}
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
      </DataCard>
      {editing
        ? props.formModal({
            open: true,
            mode: editing.mode,
            submitting: (create?.isPending ?? false) || (update?.isPending ?? false),
            onCancel: () => setEditing(null),
            onSubmit: submit,
            defaultValues: props.toForm(editing.item),
          })
        : null}
    </>
  );
}
