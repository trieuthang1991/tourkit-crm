import { Link, useNavigate } from 'react-router-dom';

/* Trang giới thiệu sản phẩm (public) — hệ token Vuexy (.mk-*). */

function Ic({ name }: { name: string }) {
  return <span className="material-symbols-outlined">{name}</span>;
}

const NAV_LINKS = [
  { href: '#tinh-nang', label: 'Tính năng' },
  { href: '#phan-he', label: 'Phân hệ' },
  { href: '#so-lieu', label: 'Số liệu' },
];

const FEATURES = [
  {
    icon: 'groups', tint: '#7367f0', wide: true,
    title: 'CRM & phễu bán hàng',
    desc: 'Quản lý cơ hội, chia số sale, chăm sóc lịch hẹn và toàn bộ data khách hàng ở một nơi. Không rơi lead, không trùng chăm sóc, đo được tỷ lệ chốt theo từng nhân viên.',
  },
  {
    icon: 'calculate', tint: '#00bad1',
    title: 'Báo giá thông minh',
    desc: 'Dựng giá Tour, Combo, GIT, Landtour, Visa với công thức chi phí rõ ràng và biên lợi nhuận tức thì.',
  },
  {
    icon: 'shopping_cart', tint: '#28c76f',
    title: 'Đơn hàng & LKH',
    desc: 'Chuyển báo giá thành đơn, gom khách theo lượt khởi hành, theo dõi chỗ giữ/bán/còn theo thời gian thực.',
  },
  {
    icon: 'assignment', tint: '#ff9f43',
    title: 'Điều hành dịch vụ',
    desc: 'Phiếu điều hành phòng, xe, hướng dẫn viên và vé máy bay — lịch điều hành trực quan dạng Gantt.',
  },
  {
    icon: 'account_balance', tint: '#ff4c51',
    title: 'Tài chính & công nợ',
    desc: 'Phiếu thu/chi, hoá đơn VAT, dòng tiền và công nợ khách/nhà cung cấp — khớp từ đơn hàng đến kế toán.',
  },
];

const MODULES = [
  { icon: 'storefront', name: 'Nhà cung cấp', desc: 'Dịch vụ, bảng giá, quỹ vé & điều khoản thanh toán.' },
  { icon: 'hotel', name: 'Booking phòng', desc: 'Quỹ phòng, allotment, hạng phòng & danh sách booking.' },
  { icon: 'flight', name: 'Vé máy bay', desc: 'Vé đoàn, vé lẻ và series quỹ vé theo nhà cung cấp.' },
  { icon: 'badge', name: 'Hướng dẫn viên', desc: 'Điều HDV, lịch phân công và báo cáo công tác phí.' },
  { icon: 'directions_car', name: 'Quản lý xe', desc: 'Kho xe, lịch điều xe và duyệt điều động.' },
  { icon: 'percent', name: 'Hoa hồng & KPIs', desc: 'Thiết lập hoa hồng, cột mốc và KPI theo phòng ban.' },
  { icon: 'campaign', name: 'Marketing', desc: 'Email/Zalo ZNS, kho mẫu và bài viết truyền thông.' },
  { icon: 'handshake', name: 'Đại lý B2B', desc: 'Danh sách đại lý, báo giá và đặt chỗ cho đối tác.' },
];

const STATS = [
  { n: '14+', l: 'Phân hệ nghiệp vụ' },
  { n: '70+', l: 'Màn hình quản trị' },
  { n: '100%', l: 'Đa chi nhánh / đa công ty' },
  { n: '24/7', l: 'Dữ liệu thời gian thực' },
];

export function LandingPage() {
  const nav = useNavigate();

  return (
    <div className="mk mk-page">
      {/* NAV */}
      <header className="mk-nav">
        <div className="mk-nav__in">
          <Link to="/gioi-thieu" className="mk-brand">
            <span className="mk-brand__mark">T</span>
            <span className="mk-brand__name">TourKit</span>
          </Link>
          <nav className="mk-nav__links">
            {NAV_LINKS.map((l) => (
              <a key={l.href} href={l.href} className="mk-nav__link">{l.label}</a>
            ))}
          </nav>
          <div className="mk-nav__cta">
            <Link to="/login" className="mk-btn mk-btn--ghost">Đăng nhập</Link>
            <Link to="/register" className="mk-btn mk-btn--primary">Dùng thử miễn phí</Link>
          </div>
        </div>
      </header>

      {/* HERO */}
      <section className="mk-hero">
        <div className="mk-hero__in">
          <div className="mk-rise">
            <span className="mk-eyebrow"><Ic name="bolt" /> Nền tảng điều hành lữ hành</span>
            <h1>Vận hành công ty tour <span className="mk-hl">gọn trong một hệ thống</span></h1>
            <p className="mk-hero__lead">
              TourKit CRM kết nối bán hàng, báo giá, điều hành dịch vụ và tài chính thành một luồng liền mạch —
              để đội của bạn phục vụ khách nhanh hơn và kiểm soát lợi nhuận chặt hơn.
            </p>
            <div className="mk-hero__actions">
              <button className="mk-btn mk-btn--primary mk-btn--lg" onClick={() => nav('/register')}>
                Bắt đầu ngay <Ic name="arrow_forward" />
              </button>
              <button className="mk-btn mk-btn--ghost mk-btn--lg" onClick={() => nav('/login')}>
                Xem bản demo
              </button>
            </div>
            <div className="mk-hero__note"><Ic name="check_circle" /> Không cần thẻ tín dụng · Cài đặt trong vài phút</div>
          </div>

          {/* mock dashboard */}
          <div className="mk-shot mk-rise" style={{ animationDelay: '.08s' }}>
            <div className="mk-shot__frame">
              <div className="mk-shot__bar">
                <span className="mk-shot__dot" style={{ background: '#ff4c51' }} />
                <span className="mk-shot__dot" style={{ background: '#ff9f43' }} />
                <span className="mk-shot__dot" style={{ background: '#28c76f' }} />
              </div>
              <div className="mk-shot__body">
                <div className="mk-shot__kpis">
                  <div className="mk-shot__kpi"><small>Doanh thu</small><b>42.1M</b></div>
                  <div className="mk-shot__kpi"><small>Đã thu</small><b style={{ color: '#28c76f' }}>20.8M</b></div>
                  <div className="mk-shot__kpi"><small>Còn nợ</small><b style={{ color: '#ff4c51' }}>21.3M</b></div>
                </div>
                <div className="mk-shot__bars">
                  <span style={{ height: '48%' }} /><span style={{ height: '30%' }} /><span className="on" style={{ height: '92%' }} />
                  <span style={{ height: '64%' }} /><span style={{ height: '40%' }} /><span style={{ height: '72%' }} />
                  <span className="on" style={{ height: '58%' }} /><span style={{ height: '34%' }} />
                </div>
              </div>
            </div>
            <div className="mk-shot__float mk-shot__float--a">
              <span className="material-symbols-outlined" style={{ background: 'rgba(40,199,111,.16)', color: '#28c76f' }}>trending_up</span>
              <span><b>+24.5%</b><small>Tổng số đơn tuần này</small></span>
            </div>
            <div className="mk-shot__float mk-shot__float--b">
              <span className="material-symbols-outlined" style={{ background: 'rgba(115,103,240,.16)', color: '#7367f0' }}>event_available</span>
              <span><b>5 chuyến</b><small>Sắp khởi hành</small></span>
            </div>
          </div>
        </div>
      </section>

      {/* TRUST */}
      <div className="mk-trust">
        <p>Được tin dùng bởi các công ty lữ hành Inbound · Outbound · Nội địa</p>
        <div className="mk-trust__row">
          <span>Inbound</span><span>Outbound</span><span>Landtour</span><span>GIT / FIT</span><span>Combo</span><span>B2B</span>
        </div>
      </div>

      {/* FEATURES */}
      <section id="tinh-nang" className="mk-sec">
        <div className="mk-sec__head">
          <p className="mk-sec__k">Tính năng cốt lõi</p>
          <h2 className="mk-sec__t">Mọi khâu vận hành, một luồng dữ liệu</h2>
          <p className="mk-sec__d">Không còn file Excel rời rạc hay số liệu lệch giữa các phòng ban — mọi thứ chảy từ khách hàng đến kế toán trong cùng một hệ thống.</p>
        </div>
        <div className="mk-bento">
          {FEATURES.map((f) => (
            <article key={f.title} className={`mk-feat${f.wide ? ' mk-feat--wide' : ''}`}>
              <div className="mk-feat__ic" style={{ background: `${f.tint}22`, color: f.tint }}><Ic name={f.icon} /></div>
              <h3>{f.title}</h3>
              <p>{f.desc}</p>
            </article>
          ))}
        </div>
      </section>

      {/* MODULES */}
      <section id="phan-he" className="mk-mods">
        <div className="mk-sec">
          <div className="mk-sec__head">
            <p className="mk-sec__k">Phân hệ nghiệp vụ</p>
            <h2 className="mk-sec__t">Đầy đủ cho công ty lữ hành</h2>
            <p className="mk-sec__d">Từ nhà cung cấp, phòng khách sạn, vé máy bay đến hướng dẫn viên và đại lý — bật đúng phân hệ bạn cần.</p>
          </div>
          <div className="mk-mods__grid">
            {MODULES.map((m) => (
              <div key={m.name} className="mk-mod">
                <div className="mk-mod__ic"><Ic name={m.icon} /></div>
                <h4>{m.name}</h4>
                <p>{m.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* STATS */}
      <section id="so-lieu" className="mk-stats">
        <div className="mk-stats__in">
          {STATS.map((s) => (
            <div key={s.l} className="mk-stat"><b>{s.n}</b><span>{s.l}</span></div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="mk-cta">
        <div className="mk-cta__box">
          <h2>Sẵn sàng tăng tốc đội tour của bạn?</h2>
          <p>Tạo tài khoản công ty và trải nghiệm toàn bộ phân hệ ngay hôm nay.</p>
          <div className="mk-hero__actions" style={{ justifyContent: 'center' }}>
            <Link to="/register" className="mk-btn mk-btn--primary mk-btn--lg">Đăng ký công ty <Ic name="arrow_forward" /></Link>
            <Link to="/login" className="mk-btn mk-btn--ghost mk-btn--lg">Đăng nhập</Link>
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer className="mk-footer">
        <div className="mk-footer__in">
          <Link to="/gioi-thieu" className="mk-brand">
            <span className="mk-brand__mark">T</span>
            <span className="mk-brand__name">TourKit</span>
          </Link>
          <nav className="mk-footer__links">
            <a href="#tinh-nang">Tính năng</a>
            <a href="#phan-he">Phân hệ</a>
            <Link to="/login">Đăng nhập</Link>
            <Link to="/register">Đăng ký</Link>
          </nav>
          <div className="mk-footer__copy">© {new Date().getFullYear()} TourKit CRM · hotro@tourkit.vn</div>
        </div>
      </footer>
    </div>
  );
}
