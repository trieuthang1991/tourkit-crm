import type { ColumnsType } from 'antd/es/table';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { ticketFundsCrud } from './ticketFundsCrud';
import { ticketFundFormSchema } from './types';
import type { TicketFund, TicketFundForm } from './types';

const columns: ColumnsType<TicketFund> = [
  { title: 'Mã đơn', dataIndex: 'orderId', key: 'orderId' },
  { title: 'Mã vé', dataIndex: 'ticketCode', key: 'ticketCode' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
  { title: 'Đóng quỹ', dataIndex: 'isClosed', key: 'isClosed', render: (v: boolean) => (v ? 'Đã đóng' : 'Chưa đóng') },
];

export function TicketFundsPage() {
  return (
    <ResourcePage<TicketFund, TicketFundForm>
      title="Quỹ vé ứng"
      columns={columns}
      crud={ticketFundsCrud}
      perms={{ create: 'ticketfund.manage', update: 'ticketfund.manage', remove: 'ticketfund.manage' }}
      toForm={(t) => ({
        orderId: t?.orderId ?? '',
        providerId: t?.providerId ?? null,
        providerServiceId: t?.providerServiceId ?? null,
        ticketCode: t?.ticketCode ?? null,
        status: t?.status ?? 0,
        isClosed: t?.isClosed ?? false,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa quỹ vé' : 'Thêm quỹ vé'}
          schema={ticketFundFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="orderId" label="Mã đơn (orderId)" required />
          <TextField name="providerId" label="Mã NCC (providerId)" />
          <TextField name="providerServiceId" label="Mã giá dịch vụ (providerServiceId)" />
          <TextField name="ticketCode" label="Mã vé" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
