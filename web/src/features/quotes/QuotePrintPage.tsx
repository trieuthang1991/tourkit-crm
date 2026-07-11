import { Button } from 'antd';
import { useParams } from 'react-router-dom';
import { money } from '../../shared/format';
import { useCompanyProfile } from '../settings/companyApi';
import { useDefaultPaymentAccount, useQuote } from './quotesApi';
import { SERVICE_TYPE_OPTIONS } from './types';

const serviceTypeLabel = (v: number) => SERVICE_TYPE_OPTIONS.find((o) => o.value === v)?.label ?? 'Khác';

/// Bản in báo giá gửi KHÁCH (thay legacy template .docx): chỉ hiện GIÁ BÁN —
/// tuyệt đối không in giá vốn/%LN/lãi (số nội bộ). Bố cục chuẩn quotation:
/// tiêu đề + mã/hạn hiệu lực, khách hàng, bảng dòng dịch vụ, giá theo hạng khách, tổng, chữ ký.
export function QuotePrintPage() {
  const { id } = useParams<{ id: string }>();
  const quote = useQuote(id ?? '');
  const account = useDefaultPaymentAccount();
  const company = useCompanyProfile();

  if (!quote.data) {
    return <div style={{ padding: 24 }}>Đang tải báo giá…</div>;
  }

  const q = quote.data;
  const co = company.data;
  const paxTotal = q.adults + q.children + q.infants;

  return (
    <div style={{ maxWidth: 800, margin: '0 auto', padding: 32, background: '#fff', color: '#000' }}>
      {/* Nút chỉ hiện trên màn hình, ẩn khi in */}
      <div className="no-print" style={{ marginBottom: 16, display: 'flex', gap: 8 }}>
        <Button type="primary" onClick={() => window.print()}>
          In / Xuất PDF
        </Button>
        <Button onClick={() => window.close()}>Đóng</Button>
      </div>
      <style>{`@media print { .no-print { display: none !important; } }`}</style>

      {co?.name ? (
        <div style={{ marginBottom: 16, borderBottom: '2px solid #333', paddingBottom: 8 }}>
          <div style={{ fontSize: 18, fontWeight: 'bold' }}>{co.name}</div>
          {co.address ? <div>{co.address}</div> : null}
          <div>
            {co.hotline ? `Hotline: ${co.hotline}` : ''}
            {co.email ? `${co.hotline ? ' · ' : ''}Email: ${co.email}` : ''}
            {co.website ? `${co.hotline || co.email ? ' · ' : ''}${co.website}` : ''}
          </div>
          {co.taxCode ? <div>MST: {co.taxCode}</div> : null}
        </div>
      ) : null}

      <div style={{ textAlign: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0, fontSize: 28 }}>BÁO GIÁ TOUR</h1>
        <div>
          Mã: <strong>{q.code}</strong>
          {q.validUntil ? <> · Hiệu lực đến: {new Date(q.validUntil).toLocaleDateString('vi-VN')}</> : null}
        </div>
      </div>

      <table style={{ width: '100%', marginBottom: 16 }}>
        <tbody>
          <tr>
            <td>
              <strong>Kính gửi:</strong> {q.customerName || '—'}
            </td>
            <td style={{ textAlign: 'right' }}>
              <strong>Chương trình:</strong> {q.title}
            </td>
          </tr>
          {paxTotal > 0 ? (
            <tr>
              <td colSpan={2}>
                <strong>Số khách:</strong> {q.adults} người lớn
                {q.children > 0 ? ` · ${q.children} trẻ em` : ''}
                {q.infants > 0 ? ` · ${q.infants} trẻ nhỏ` : ''}
              </td>
            </tr>
          ) : null}
        </tbody>
      </table>

      <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 16 }} border={1} cellPadding={6}>
        <thead>
          <tr style={{ background: '#f5f5f5' }}>
            <th>STT</th>
            <th>Dịch vụ</th>
            <th>Loại</th>
            <th>Phạm vi</th>
            <th>SL</th>
            <th>Đơn giá</th>
            <th>Thành tiền</th>
          </tr>
        </thead>
        <tbody>
          {q.lines.map((l, i) => (
            <tr key={l.id}>
              <td style={{ textAlign: 'center' }}>{i + 1}</td>
              <td>{l.description}</td>
              <td>{serviceTypeLabel(l.serviceType)}</td>
              <td>{l.scope === 1 ? 'Theo khách' : 'Cả đoàn'}</td>
              <td style={{ textAlign: 'center' }}>{l.quantity}</td>
              <td style={{ textAlign: 'right' }}>{money(l.unitPrice)}</td>
              <td style={{ textAlign: 'right' }}>{money(l.amount)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {paxTotal > 0 ? (
        <table style={{ width: '60%', borderCollapse: 'collapse', marginBottom: 16 }} border={1} cellPadding={6}>
          <thead>
            <tr style={{ background: '#f5f5f5' }}>
              <th>Hạng khách</th>
              <th>Giá/khách</th>
              <th>SL</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Người lớn</td>
              <td style={{ textAlign: 'right' }}>{money(q.adultPrice)}</td>
              <td style={{ textAlign: 'center' }}>{q.adults}</td>
            </tr>
            {q.children > 0 ? (
              <tr>
                <td>Trẻ em ({q.childPercent}%)</td>
                <td style={{ textAlign: 'right' }}>{money(q.childPrice)}</td>
                <td style={{ textAlign: 'center' }}>{q.children}</td>
              </tr>
            ) : null}
            {q.infants > 0 ? (
              <tr>
                <td>Trẻ nhỏ ({q.infantPercent}%)</td>
                <td style={{ textAlign: 'right' }}>{money(q.infantPrice)}</td>
                <td style={{ textAlign: 'center' }}>{q.infants}</td>
              </tr>
            ) : null}
          </tbody>
        </table>
      ) : null}

      <div style={{ textAlign: 'right', fontSize: 18, marginBottom: 24 }}>
        <strong>TỔNG CỘNG: {money(q.totalAmount)}</strong>
      </div>

      {q.note ? (
        <div style={{ marginBottom: 24 }}>
          <strong>Ghi chú:</strong> {q.note}
        </div>
      ) : null}

      {account.data ? (
        <div style={{ marginBottom: 24, padding: 12, border: '1px solid #ddd' }}>
          <div>
            <strong>Thông tin thanh toán:</strong>
          </div>
          <div>{account.data.name}</div>
          {account.data.bankName ? <div>Ngân hàng: {account.data.bankName}</div> : null}
          {account.data.accountNumber ? <div>Số tài khoản: {account.data.accountNumber}</div> : null}
          {account.data.accountHolder ? <div>Chủ tài khoản: {account.data.accountHolder}</div> : null}
          {account.data.branch ? <div>Chi nhánh: {account.data.branch}</div> : null}
          {account.data.transferNote ? <div>Nội dung CK: {account.data.transferNote}</div> : null}
        </div>
      ) : null}

      <table style={{ width: '100%', marginTop: 48 }}>
        <tbody>
          <tr style={{ textAlign: 'center' }}>
            <td>
              <strong>NGƯỜI LẬP BÁO GIÁ</strong>
              <div style={{ height: 80 }} />
            </td>
            <td>
              <strong>XÁC NHẬN CỦA KHÁCH HÀNG</strong>
              <div style={{ height: 80 }} />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}
