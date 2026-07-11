import { App, Button, Card, Input, Popconfirm, Select, Space, Tag, Typography } from 'antd';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { priorityLabel } from '../workTasks/types';
import { useAddCard, useAddSection, useDeleteSection, useMoveTask, useWorkflowBoard } from './api';
import type { BoardColumn } from './types';

function AddCard({ workflowId, sectionId }: { workflowId: string; sectionId: string }) {
  const { message } = App.useApp();
  const add = useAddCard(workflowId);
  const [title, setTitle] = useState('');

  async function submit() {
    if (!title.trim()) return;
    try {
      await add.mutateAsync({ title, sectionId });
      setTitle('');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <Space.Compact style={{ width: '100%', marginTop: 8 }}>
      <Input
        size="small"
        placeholder="Thêm thẻ việc…"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        onPressEnter={submit}
      />
      <Button size="small" type="primary" loading={add.isPending} onClick={submit}>
        +
      </Button>
    </Space.Compact>
  );
}

export function WorkflowBoardPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('workflow.manage');
  const { id } = useParams<{ id: string }>();
  const boardId = id ?? '';
  const board = useWorkflowBoard(boardId);
  const addSection = useAddSection(boardId);
  const deleteSection = useDeleteSection(boardId);
  const moveTask = useMoveTask(boardId);

  async function handleAddSection() {
    const name = window.prompt('Tên cột mới:');
    if (!name?.trim()) return;
    try {
      await addSection.mutateAsync(name.trim());
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns = board.data?.columns ?? [];
  const columnOptions = columns.map((c) => ({ label: c.section.name, value: c.section.id }));

  return (
    <>
      <PageHeader
        title={board.data?.name ?? 'Board'}
        extra={
          <Space>
            <Link to="/workflows">
              <Button>← Danh sách board</Button>
            </Link>
            {canManage ? (
              <Button type="primary" loading={addSection.isPending} onClick={handleAddSection}>
                Thêm cột
              </Button>
            ) : null}
          </Space>
        }
      />
      <div style={{ display: 'flex', gap: 16, overflowX: 'auto', paddingBottom: 8, alignItems: 'flex-start' }}>
        {columns.map((col: BoardColumn) => (
          <Card
            key={col.section.id}
            size="small"
            style={{ minWidth: 280, maxWidth: 280, flex: '0 0 auto' }}
            title={
              <Space>
                <span>{col.section.name}</span>
                <Tag>{col.tasks.length}</Tag>
              </Space>
            }
            extra={
              canManage && col.section.allowDelete ? (
                <Popconfirm
                  title="Xoá cột? (thẻ việc sẽ được tách khỏi cột)"
                  onConfirm={() => deleteSection.mutate(col.section.id)}
                >
                  <Button size="small" type="text" danger>
                    ✕
                  </Button>
                </Popconfirm>
              ) : null
            }
          >
            <Space direction="vertical" style={{ width: '100%' }} size={8}>
              {col.tasks.map((task) => (
                <Card key={task.id} size="small" styles={{ body: { padding: 8 } }}>
                  <Typography.Text>{task.title}</Typography.Text>
                  <div style={{ marginTop: 4 }}>
                    <Tag color={task.priority === 2 ? 'red' : task.priority === 0 ? 'default' : 'blue'}>
                      {priorityLabel(task.priority)}
                    </Tag>
                    {task.assigneeName ? <Tag>{task.assigneeName}</Tag> : null}
                  </div>
                  {canManage ? (
                    <Select
                      size="small"
                      style={{ width: '100%', marginTop: 6 }}
                      value={col.section.id}
                      options={columnOptions}
                      onChange={(sectionId) => {
                        if (sectionId !== col.section.id) {
                          moveTask.mutate({ taskId: task.id, sectionId });
                        }
                      }}
                    />
                  ) : null}
                </Card>
              ))}
              {canManage ? <AddCard workflowId={boardId} sectionId={col.section.id} /> : null}
            </Space>
          </Card>
        ))}
        {columns.length === 0 && !board.isLoading ? <Typography.Text type="secondary">Chưa có cột.</Typography.Text> : null}
      </div>
    </>
  );
}
