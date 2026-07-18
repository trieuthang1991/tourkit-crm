import { App, Button, List, Tag } from '../../shared/ui/antd';
import { useNavigate } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { Icon } from '../../ui/kit';
import { useMarkAllRead, useMarkRead, useNotifications } from './api';

// Phân loại thông báo → icon + màu + nhãn (khớp type do backend gán: approval/task/marketing/system).
const SYSTEM_META = { icon: 'notifications', color: 'default', label: 'Hệ thống' };
const TYPE_META: Record<string, { icon: string; color: string; label: string }> = {
  approval: { icon: 'fact_check', color: 'blue', label: 'Duyệt' },
  task: { icon: 'assignment', color: 'orange', label: 'Công việc' },
  marketing: { icon: 'campaign', color: 'purple', label: 'Marketing' },
  system: SYSTEM_META,
};
const typeMeta = (t: string) => TYPE_META[t] ?? SYSTEM_META;

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
            style={{ cursor: 'pointer', background: n.isRead ? undefined : 'var(--tk-info-soft-2)' }}
            onClick={() => open(n.id, n.linkUrl, n.isRead)}
            actions={[
              <Tag key="type" color={typeMeta(n.type).color}>{typeMeta(n.type).label}</Tag>,
              ...(n.isRead ? [] : [<Tag key="new" color="blue">Mới</Tag>]),
            ]}
          >
            <List.Item.Meta
              avatar={<Icon name={typeMeta(n.type).icon} size={20} />}
              title={n.title}
              description={
                <>
                  {n.message ? <div>{n.message}</div> : null}
                  <div style={{ color: 'var(--tk-muted)', fontSize: 12 }}>{new Date(n.createdAt).toLocaleString('vi-VN')}</div>
                </>
              }
            />
          </List.Item>
        )}
      />
    </>
  );
}
