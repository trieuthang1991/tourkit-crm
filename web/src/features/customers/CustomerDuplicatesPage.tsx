import { Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { DataCard } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useCustomerDuplicates } from './customerDuplicatesApi';
import type { DuplicateGroup } from './customerDuplicatesApi';

/* Màn "Rà khách trùng" (/customers/duplicates): gom khách nghi trùng theo SĐT/email đã chuẩn hoá
   để nhân viên rà soát và gộp thủ công. Chỉ hiển thị nhóm ≥2 khách. */

type Row = DuplicateGroup['customers'][number];

const dateVi = (v: string) => new Date(v).toLocaleDateString('vi-VN');

function GroupCard({ group }: { group: DuplicateGroup }) {
  const columns: ColumnsType<Row> = [
    { title: 'Mã KH', dataIndex: 'code', key: 'code', render: (v: string | null) => v ?? '—' },
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    { title: 'SĐT', dataIndex: 'phone', key: 'phone', render: (v: string | null) => v ?? '—' },
    { title: 'Email', dataIndex: 'email', key: 'email', render: (v: string | null) => v ?? '—' },
    { title: 'Ngày tạo', dataIndex: 'createdAt', key: 'createdAt', render: (v: string) => dateVi(v) },
  ];

  const label = group.matchType === 'phone' ? 'Trùng SĐT' : 'Trùng email';
  return (
    <DataCard
      title={
        <span style={{ display: 'inline-flex', gap: 8, alignItems: 'center' }}>
          <Tag color={group.matchType === 'phone' ? 'blue' : 'purple'}>{label}</Tag>
          <span style={{ fontFamily: 'var(--tk-font-mono)' }}>{group.matchKey}</span>
          <span style={{ color: 'var(--tk-muted)', fontWeight: 400 }}>· {group.customers.length} khách</span>
        </span>
      }
    >
      <Table rowKey="id" columns={columns} dataSource={group.customers} pagination={false} size="small" />
    </DataCard>
  );
}

export function CustomerDuplicatesPage() {
  const dup = useCustomerDuplicates();
  const groups = dup.data ?? [];

  return (
    <>
      <PageHeader
        title="Rà khách trùng"
        desc="Gom khách nghi trùng theo số điện thoại / email đã chuẩn hoá để rà soát và gộp thủ công."
      />
      {dup.isLoading ? (
        <DataCard>
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--tk-muted)' }}>Đang tải…</div>
        </DataCard>
      ) : groups.length === 0 ? (
        <DataCard>
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--tk-muted)' }}>
            Không phát hiện khách trùng theo SĐT/email. 🎉
          </div>
        </DataCard>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {groups.map((g) => (
            <GroupCard key={`${g.matchType}:${g.matchKey}`} group={g} />
          ))}
        </div>
      )}
    </>
  );
}
