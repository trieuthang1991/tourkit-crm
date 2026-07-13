import { Card, Col, Row, Typography } from 'antd';
import type { ReactNode } from 'react';
import {
  ApartmentOutlined,
  BankOutlined,
  CarOutlined,
  DollarOutlined,
  GlobalOutlined,
  HomeOutlined,
  IdcardOutlined,
  PercentageOutlined,
  SolutionOutlined,
  TagsOutlined,
  TeamOutlined,
  TranslationOutlined,
  UsergroupAddOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

type HubItem = { to: string; title: string; desc: string; icon: ReactNode; perm: string };
type HubSection = { title: string; items: HubItem[] };

// Bám hub "Thiết lập hệ thống" (/config) hệ cũ: gom catalog theo 4 nhóm. Chỉ hiện mục ĐÃ có màn local.
const SECTIONS: HubSection[] = [
  {
    title: 'Cài đặt chung',
    items: [
      { to: '/company-profile', title: 'Cấu hình công ty', desc: 'Cài đặt thông tin công ty', icon: <ApartmentOutlined />, perm: 'company.manage' },
      { to: '/market-types', title: 'Thị trường', desc: 'Quản lý thị trường (cha-con)', icon: <GlobalOutlined />, perm: 'market.view' },
      { to: '/departments', title: 'Nhóm / Phòng ban', desc: 'Quản lý nhóm nhân viên', icon: <UsergroupAddOutlined />, perm: 'user.view' },
      { to: '/positions', title: 'Chức vụ', desc: 'Quản lý chức vụ nhân viên', icon: <SolutionOutlined />, perm: 'user.view' },
      { to: '/approval-processes', title: 'Quy trình duyệt', desc: 'Quản lý quy trình phê duyệt', icon: <TeamOutlined />, perm: 'approvalprocess.view' },
    ],
  },
  {
    title: 'Thông tin khách hàng',
    items: [
      { to: '/customer-types', title: 'Phân loại khách hàng', desc: 'Danh sách phân loại khách hàng', icon: <IdcardOutlined />, perm: 'customertype.view' },
      { to: '/customer-sources', title: 'Nguồn khách hàng', desc: 'Danh mục nguồn khách hàng', icon: <GlobalOutlined />, perm: 'customertype.view' },
      { to: '/customer-tags', title: 'Thẻ khách hàng', desc: 'Danh mục thẻ/nhãn khách hàng', icon: <TagsOutlined />, perm: 'customertype.view' },
      { to: '/transfer-reasons', title: 'Lý do hủy / chuyển', desc: 'Danh mục lý do hủy/chuyển', icon: <SolutionOutlined />, perm: 'booking.view' },
    ],
  },
  {
    title: 'Thông tin nhà cung cấp',
    items: [
      { to: '/room-classes', title: 'Class Hotel', desc: 'Thiết lập class Hotel (hạng phòng)', icon: <HomeOutlined />, perm: 'servicebooking.view' },
      { to: '/car-types', title: 'Loại xe', desc: 'Cấu hình loại xe trong điều xe', icon: <CarOutlined />, perm: 'vehicle.view' },
      { to: '/language-types', title: 'Ngôn ngữ', desc: 'Cấu hình ngôn ngữ HDV', icon: <TranslationOutlined />, perm: 'guide.view' },
    ],
  },
  {
    title: 'Tài chính',
    items: [
      { to: '/payment-accounts', title: 'Phương thức thanh toán', desc: 'Danh mục tài khoản nhận tiền', icon: <BankOutlined />, perm: 'paymentaccount.view' },
      { to: '/currencies', title: 'Tỉ giá', desc: 'Thiết lập tỉ giá tiền tệ', icon: <DollarOutlined />, perm: 'service.view' },
      { to: '/surcharges', title: 'Quản lý Phụ thu', desc: 'Danh mục các loại phụ thu', icon: <PercentageOutlined />, perm: 'booking.view' },
    ],
  },
];

export function ConfigHubPage() {
  const navigate = useNavigate();
  const { has } = useAuth();

  return (
    <>
      <Typography.Title level={3} style={{ marginTop: 0 }}>Thiết lập hệ thống</Typography.Title>
      {SECTIONS.map((section) => {
        const items = section.items.filter((i) => has(i.perm));
        if (!items.length) return null;
        return (
          <Card key={section.title} title={section.title} style={{ marginBottom: 16 }} styles={{ body: { paddingBottom: 4 } }}>
            <Row gutter={[16, 16]}>
              {items.map((i) => (
                <Col key={i.to} xs={24} sm={12} lg={8}>
                  <Card hoverable size="small" onClick={() => navigate(i.to)} styles={{ body: { display: 'flex', gap: 12, alignItems: 'center' } }}>
                    <span style={{ fontSize: 22, color: '#EB5324' }}>{i.icon}</span>
                    <span>
                      <div style={{ fontWeight: 600 }}>{i.title}</div>
                      <div style={{ fontSize: 12, color: '#888' }}>{i.desc}</div>
                    </span>
                  </Card>
                </Col>
              ))}
            </Row>
          </Card>
        );
      })}
    </>
  );
}
