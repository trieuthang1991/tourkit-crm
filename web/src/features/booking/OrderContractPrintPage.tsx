import { Button } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';

const contractSchema = z.object({
  orderCode: z.string(),
  customerName: z.string(),
  customerPhone: z.string().nullable(),
  customerAddress: z.string().nullable(),
  customerIdCard: z.string().nullable(),
  customerPassport: z.string().nullable(),
  tourTitle: z.string(),
  departureDate: z.string().nullable(),
  endDate: z.string().nullable(),
  adultCount: z.number(),
  childCount: z.number(),
  infantCount: z.number(),
  totalRevenue: z.number(),
  terms: z.string().nullable(),
});

const d = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '…');

/// Bản in hợp đồng dịch vụ du lịch (thay legacy contract_tour): render cố định từ dữ liệu đơn.
/// Cần đăng nhập nhưng NGOÀI AppShell để trang in sạch.
export function OrderContractPrintPage() {
  const { id } = useParams<{ id: string }>();
  const contract = useQuery({
    queryKey: ['orders', id, 'contract'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${id}/contract`);
      return contractSchema.parse(data);
    },
    enabled: !!id,
  });

  if (!contract.data) {
    return <div style={{ padding: 24 }}>Đang tải hợp đồng…</div>;
  }

  const c = contract.data;

  return (
    <div style={{ maxWidth: 800, margin: '0 auto', padding: 32, background: '#fff', color: '#000' }}>
      <div className="no-print" style={{ marginBottom: 16, display: 'flex', gap: 8 }}>
        <Button type="primary" onClick={() => window.print()}>
          In / Xuất PDF
        </Button>
        <Button onClick={() => window.close()}>Đóng</Button>
      </div>
      <style>{`@media print { .no-print { display: none !important; } }`}</style>

      <div style={{ textAlign: 'center', marginBottom: 24 }}>
        <div>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div>
        <div>Độc lập - Tự do - Hạnh phúc</div>
        <h1 style={{ fontSize: 24, marginTop: 16 }}>HỢP ĐỒNG DỊCH VỤ DU LỊCH</h1>
        <div>Số: {c.orderCode}</div>
      </div>

      <p>
        <strong>BÊN A (Bên cung cấp dịch vụ):</strong> Công ty du lịch …
      </p>
      <p>
        <strong>BÊN B (Khách hàng):</strong> {c.customerName}
        {c.customerPhone ? ` · ĐT: ${c.customerPhone}` : ''}
        {c.customerAddress ? ` · Địa chỉ: ${c.customerAddress}` : ''}
        {c.customerIdCard ? ` · CMND/CCCD: ${c.customerIdCard}` : ''}
        {c.customerPassport ? ` · Hộ chiếu: ${c.customerPassport}` : ''}
      </p>

      <h3>Điều 1: Nội dung chương trình</h3>
      <p>
        Chương trình: <strong>{c.tourTitle}</strong>
        <br />
        Thời gian: {d(c.departureDate)} — {d(c.endDate)}
        <br />
        Số khách: {c.adultCount} người lớn
        {c.childCount > 0 ? ` · ${c.childCount} trẻ em` : ''}
        {c.infantCount > 0 ? ` · ${c.infantCount} trẻ nhỏ` : ''}
      </p>

      <h3>Điều 2: Giá trị hợp đồng</h3>
      <p>
        Tổng giá trị: <strong>{money(c.totalRevenue)}</strong>
      </p>

      <h3>Điều 3: Điều khoản</h3>
      <p style={{ whiteSpace: 'pre-wrap' }}>{c.terms ?? 'Theo thỏa thuận giữa hai bên.'}</p>

      <table style={{ width: '100%', marginTop: 48 }}>
        <tbody>
          <tr style={{ textAlign: 'center' }}>
            <td>
              <strong>ĐẠI DIỆN BÊN A</strong>
              <div style={{ height: 80 }} />
            </td>
            <td>
              <strong>ĐẠI DIỆN BÊN B</strong>
              <div style={{ height: 80 }} />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}
