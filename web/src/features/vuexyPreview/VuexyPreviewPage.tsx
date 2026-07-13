import { useNavigate } from 'react-router-dom';
import { Badge, Button, Card, CardBody, CardHeader, Col, Input, Row, Table } from 'reactstrap';

// Trang XEM THỬ diện mạo Vuexy (Bootstrap/reactstrap) — bọc trong .vuexy-scope nên Bootstrap
// KHÔNG đè các màn AntD khác. Đây là MẪU để chủ dự án duyệt trước khi migrate toàn app.
// Xem docs/vuexy-migration.md.

const MENU = [
  { icon: '🏠', label: 'Bàn làm việc', active: true },
  { icon: '🏢', label: 'Nhà cung cấp' },
  { icon: '👥', label: 'CRM' },
  { icon: '🧮', label: 'Báo giá' },
  { icon: '🛒', label: 'Đơn hàng / LKH' },
  { icon: '🧾', label: 'Booking & Dịch vụ' },
  { icon: '🚌', label: 'Điều xe & HDV' },
  { icon: '🏦', label: 'Tài chính / Kế toán' },
  { icon: '📊', label: 'Báo cáo' },
  { icon: '⚙️', label: 'Cài đặt hệ thống' },
];

const STATS = [
  { icon: '👤', label: 'Tổng số khách hàng', value: '22,036', bg: 'rgba(235,83,36,.12)', color: '#eb5324' },
  { icon: '🆕', label: 'Tạo trong tháng', value: '412', bg: 'rgba(40,199,111,.12)', color: '#28c76f' },
  { icon: '💰', label: 'Doanh thu tháng', value: '1.24 tỷ', bg: 'rgba(0,207,232,.12)', color: '#00cfe8' },
  { icon: '📌', label: 'Công nợ phải thu', value: '385 tr', bg: 'rgba(255,159,67,.12)', color: '#ff9f43' },
];

const ROWS = [
  { code: 'KH_00041127', name: 'Trần Thị Hồng', phone: '0912345678', type: 'Cá nhân', rev: '52,000,000', st: 'Mua lại', color: 'success' },
  { code: 'KH_00041104', name: 'Nguyễn Văn A', phone: '0827894350', type: 'Doanh nghiệp', rev: '0', st: 'Tiềm năng', color: 'warning' },
  { code: 'KH_00041081', name: 'Lê Văn Bình', phone: '0987657687', type: 'Đối tác', rev: '141,261,870', st: 'Mua lại', color: 'success' },
  { code: 'KH058201', name: 'Phạm Thu Trang', phone: '0877731414', type: 'CTV', rev: '0', st: 'Mới', color: 'info' },
];

export function VuexyPreviewPage() {
  const navigate = useNavigate();
  return (
    <div className="vuexy-scope vx-shell d-flex">
      {/* Sidebar */}
      <aside className="vx-sidebar d-flex flex-column flex-shrink-0">
        <div className="d-flex align-items-center gap-2 px-3" style={{ height: 62 }}>
          <span
            className="d-inline-flex align-items-center justify-content-center text-white fw-bold"
            style={{ width: 34, height: 34, borderRadius: 8, background: 'linear-gradient(135deg,#eb5324,#c73e17)' }}
          >
            T
          </span>
          <span className="fw-bold fs-5" style={{ color: '#5e5873' }}>
            TourKit
          </span>
        </div>
        <ul className="nav flex-column px-2 pt-1" style={{ gap: 2 }}>
          {MENU.map((m) => (
            <li key={m.label} className="nav-item">
              <a
                href="#"
                onClick={(e) => e.preventDefault()}
                className="nav-link d-flex align-items-center gap-2 rounded"
                style={
                  m.active
                    ? { background: 'linear-gradient(118deg,#eb5324,rgba(235,83,36,.7))', color: '#fff', boxShadow: '0 0 10px 1px rgba(235,83,36,.5)' }
                    : { color: '#6e6b7b' }
                }
              >
                <span>{m.icon}</span>
                <span>{m.label}</span>
              </a>
            </li>
          ))}
        </ul>
      </aside>

      {/* Main */}
      <div className="flex-grow-1 d-flex flex-column" style={{ minWidth: 0 }}>
        {/* Navbar */}
        <nav
          className="d-flex align-items-center gap-3 px-3 bg-white"
          style={{ height: 62, boxShadow: '0 4px 24px 0 rgba(34,41,47,.1)', borderRadius: '0 0 6px 6px', margin: 8 }}
        >
          <Input placeholder="🔍  Tìm khách hàng, tour nhanh..." style={{ maxWidth: 360, borderRadius: 20 }} bsSize="sm" />
          <div className="ms-auto d-flex align-items-center gap-3">
            <span style={{ fontSize: 18 }}>🔔</span>
            <div className="d-flex align-items-center gap-2">
              <span
                className="d-inline-flex align-items-center justify-content-center text-white"
                style={{ width: 34, height: 34, borderRadius: '50%', background: '#eb5324' }}
              >
                A
              </span>
              <div className="d-none d-md-block lh-1">
                <div className="fw-semibold" style={{ color: '#5e5873', fontSize: 14 }}>
                  Admin Tourkit
                </div>
                <small className="text-muted">Giám đốc</small>
              </div>
            </div>
          </div>
        </nav>

        <div className="p-3" style={{ overflow: 'auto' }}>
          <div className="d-flex align-items-center mb-3">
            <h4 className="mb-0" style={{ color: '#5e5873' }}>
              Data khách hàng
            </h4>
            <Button color="primary" className="ms-auto" size="sm">
              + Thêm mới
            </Button>
          </div>

          {/* Stat cards */}
          <Row className="g-3 mb-1">
            {STATS.map((s) => (
              <Col key={s.label} xs="6" lg="3">
                <Card className="vx-card">
                  <CardBody className="d-flex align-items-center gap-3">
                    <span className="vx-stat-icon" style={{ background: s.bg, color: s.color, fontSize: 20 }}>
                      {s.icon}
                    </span>
                    <div>
                      <h4 className="mb-0 fw-bolder" style={{ color: '#5e5873' }}>
                        {s.value}
                      </h4>
                      <small className="text-muted">{s.label}</small>
                    </div>
                  </CardBody>
                </Card>
              </Col>
            ))}
          </Row>

          {/* Table card */}
          <Card className="vx-card mt-3">
            <CardHeader className="d-flex align-items-center">
              <h5 className="mb-0" style={{ color: '#5e5873' }}>
                Danh sách khách hàng
              </h5>
              <div className="ms-auto d-flex gap-2">
                <Input bsSize="sm" placeholder="Tìm mã / tên / SĐT" style={{ width: 220 }} />
                <Input type="select" bsSize="sm" style={{ width: 150 }}>
                  <option>Tất cả loại</option>
                  <option>Cá nhân</option>
                  <option>Doanh nghiệp</option>
                </Input>
              </div>
            </CardHeader>
            <div className="table-responsive">
              <Table hover className="mb-0 align-middle">
                <thead style={{ background: '#f3f2f7' }}>
                  <tr style={{ textTransform: 'uppercase', fontSize: 12, letterSpacing: '.5px' }}>
                    <th>Mã KH</th>
                    <th>Họ và tên</th>
                    <th>Điện thoại</th>
                    <th>Loại KH</th>
                    <th className="text-end">Doanh thu</th>
                    <th>Trạng thái</th>
                  </tr>
                </thead>
                <tbody>
                  {ROWS.map((r) => (
                    <tr key={r.code}>
                      <td className="fw-semibold" style={{ color: '#eb5324' }}>
                        {r.code}
                      </td>
                      <td>{r.name}</td>
                      <td>{r.phone}</td>
                      <td>{r.type}</td>
                      <td className="text-end">{r.rev}</td>
                      <td>
                        <Badge color={r.color} pill>
                          {r.st}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </div>
          </Card>

          <div className="mt-3">
            <Button color="secondary" outline size="sm" onClick={() => navigate('/customers')}>
              ← Về app hiện tại (Ant Design)
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
