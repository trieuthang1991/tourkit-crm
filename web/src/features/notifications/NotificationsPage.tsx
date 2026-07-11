import { App, Button, List, Tag } from 'antd';
import { useNavigate } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useMarkAllRead, useMarkRead, useNotifications } from './api';

export function NotificationsPage() {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const list = useNotifications();
  const markRead = useMarkRead();
  const markAll = useMarkAllRead();

  async function open(id: string, linkUrl: string | null, isRead: boolean) {
    try {
      if (!isRead) await markRead.mutateAsync(id);
      if (linkUrl) navigate(linkUrl);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <PageHeader
        title="Thông báo"
        extra={
          <Button onClick={() => markAll.mutateAsync().catch((e) => message.error(errorMessage(e)))} loading={markAll.isPending}>
            Đánh dấu đã đọc tất cả
          </Button>
        }
      />
      <List
        loading={list.isLoading}
        dataSource={list.data ?? []}
        locale={{ emptyText: 'Chưa có thông báo' }}
        renderItem={(n) => (
          <List.Item
            style={{ cursor: 'pointer', background: n.isRead ? undefined : '#e6f4ff' }}
            onClick={() => open(n.id, n.linkUrl, n.isRead)}
            actions={n.isRead ? [] : [<Tag key="new" color="blue">Mới</Tag>]}
          >
            <List.Item.Meta
              title={n.title}
              description={
                <>
                  {n.message ? <div>{n.message}</div> : null}
                  <div style={{ color: '#999', fontSize: 12 }}>{new Date(n.createdAt).toLocaleString('vi-VN')}</div>
                </>
              }
            />
          </List.Item>
        )}
      />
    </>
  );
}
