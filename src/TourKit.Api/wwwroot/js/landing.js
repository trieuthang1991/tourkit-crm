/* M-Travel landing — logic: tab vai trò, bảng giá, FAQ, hiệu ứng hover */
(function () {
  'use strict';

  /* ---------------- Dữ liệu ---------------- */

  var ROLES = [
    { name: 'Giám đốc', title: 'Biết công ty đang lãi hay lỗ, ngay hôm nay.',
      body: 'Không đợi cuối tháng. Doanh thu, dòng tiền và biên lợi nhuận của từng tour cập nhật theo thời gian thực.',
      points: [
        { h: 'Một màn hình cho cả công ty', d: 'Doanh thu, công nợ, lợi nhuận thực tế trong cùng một khung nhìn.' },
        { h: 'Đi từ tổng quan xuống chi tiết', d: 'Thấy số lạ thì bấm vào, ra tận đơn hàng và người phụ trách.' },
        { h: 'Cảnh báo trước khi thành vấn đề', d: 'Tour âm biên, công nợ quá hạn, đơn thiếu dịch vụ đều nổi lên.' }
      ],
      foot: 'Trung bình khách hàng rút thời gian làm báo cáo tháng từ 2 ngày xuống 3 giờ.',
      screen: 'Lợi nhuận theo tour', colA: 'Tour', colB: 'Trạng thái', colC: 'Biên LN',
      rows: [
        { a: 'Hàn Quốc 6N5Đ · 42 khách', b: 'Đang chạy', c: '7,2%' },
        { a: 'Đà Nẵng MICE · 120 khách', b: 'Chờ quyết toán', c: '11,4%' },
        { a: 'Nhật mùa lá đỏ · 28 khách', b: 'Đang bán', c: '9,8%' },
        { a: 'Côn Đảo tri ân · 60 khách', b: 'Đã đóng', c: '4,1%' }
      ],
      stats: [{ k: 'Doanh thu quý', v: '94,3 tỷ' }, { k: 'Biên trung bình', v: '6,8%' }, { k: 'Tour đang chạy', v: '37' }] },

    { name: 'Điều hành tour', title: 'Mỗi tour một dòng chảy, không ai bị bỏ quên.',
      body: 'Từ lúc chốt đơn tới lúc đoàn về: dịch vụ, xe, HDV, khách sạn đều gắn vào đúng tour và đúng người phụ trách.',
      points: [
        { h: 'Đặt dịch vụ ngay trong tour', d: 'Khách sạn, vé máy bay, xe, HDV — đặt và theo dõi xác nhận tại chỗ.' },
        { h: 'Nhắc việc theo mốc khởi hành', d: 'Hệ thống tự dựng checklist theo ngày đi, không phải nhớ tay.' },
        { h: 'Nhà cung cấp có hồ sơ', d: 'Giá, hợp đồng và lịch sử hợp tác nằm cùng một chỗ.' }
      ],
      foot: 'Điều hành viên xử lý nhiều hơn 40% số đoàn mỗi tháng mà không tăng người.',
      screen: 'Điều hành tuần này', colA: 'Đoàn', colB: 'Dịch vụ', colC: 'Khởi hành',
      rows: [
        { a: 'ĐN-0912 · 42 khách', b: 'Thiếu 1 HDV', c: '12/09' },
        { a: 'HAN-0914 · 18 khách', b: 'Đủ', c: '14/09' },
        { a: 'PQC-0918 · 60 khách', b: 'Chờ xác nhận KS', c: '18/09' },
        { a: 'JPN-0925 · 28 khách', b: 'Đủ', c: '25/09' }
      ],
      stats: [{ k: 'Đoàn trong tuần', v: '9' }, { k: 'Việc quá hạn', v: '2' }, { k: 'NCC hoạt động', v: '146' }] },

    { name: 'Kế toán', title: 'Thu chi bám theo nghiệp vụ, không phải nhập lại.',
      body: 'Mỗi khoản thu chi đều gắn với đơn hàng, tour và người tạo. Đối chiếu công nợ không còn là việc của cả tuần.',
      points: [
        { h: 'Công nợ gắn vào đơn hàng', d: 'Biết ngay khoản nào của khách nào, ai đang theo, quá hạn bao lâu.' },
        { h: 'Quyết toán tour tự tổng hợp', d: 'Chi phí thực tế đối chiếu với dự toán ngay khi đoàn kết thúc.' },
        { h: 'Hoa hồng tính theo quy tắc', d: 'Cấu hình một lần, hệ thống tính cho từng sales mỗi kỳ.' }
      ],
      foot: 'Vòng thu công nợ trung bình rút ngắn 11 ngày sau quý đầu tiên.',
      screen: 'Công nợ phải thu', colA: 'Khách hàng', colB: 'Quá hạn', colC: 'Số tiền',
      rows: [
        { a: 'Vietravel HCM', b: '12 ngày', c: '1,80 tỷ' },
        { a: 'Cty CP Hoàng Gia', b: '5 ngày', c: '640 tr' },
        { a: 'Trường ĐH Bách Khoa', b: 'Trong hạn', c: '325 tr' },
        { a: 'Ngân hàng An Bình', b: '21 ngày', c: '2,10 tỷ' }
      ],
      stats: [{ k: 'Phải thu', v: '75,7 tỷ' }, { k: 'Quá hạn', v: '8,4 tỷ' }, { k: 'Đã thu quý', v: '18,7 tỷ' }] },

    { name: 'Sales & CSKH', title: 'Khách nào cũng có lịch sử, kể cả khi bạn nghỉ phép.',
      body: 'Toàn bộ trao đổi, báo giá và chuyến đi cũ của khách nằm trong một hồ sơ — thuộc về công ty, không nằm trong máy cá nhân.',
      points: [
        { h: 'Báo giá dựng trong vài phút', d: 'Chọn chương trình mẫu, hệ thống tính giá và biên lợi nhuận ngay.' },
        { h: 'Cơ hội không rơi giữa chừng', d: 'Mỗi khách có trạng thái, hạn theo dõi và người phụ trách rõ ràng.' },
        { h: 'Khách cũ quay lại dễ chốt hơn', d: 'Thấy ngay họ từng đi đâu, đi với ai, ngân sách bao nhiêu.' }
      ],
      foot: 'Tỷ lệ chốt của khách quay lại cao hơn 2,3 lần khi sales có đủ lịch sử.',
      screen: 'Cơ hội đang theo', colA: 'Khách hàng', colB: 'Giai đoạn', colC: 'Giá trị',
      rows: [
        { a: 'Cty Dệt may Phong Phú', b: 'Đã gửi báo giá', c: '1,45 tỷ' },
        { a: 'Gia đình a. Tuấn', b: 'Tư vấn', c: '186 tr' },
        { a: 'Sở GD Bình Dương', b: 'Đàm phán', c: '920 tr' },
        { a: 'Nhóm bạn c. Linh', b: 'Chờ cọc', c: '240 tr' }
      ],
      stats: [{ k: 'Cơ hội mở', v: '48' }, { k: 'Tỷ lệ chốt', v: '31%' }, { k: 'Báo giá tháng', v: '112' }] },

    { name: 'Nhà đầu tư', title: 'Số liệu đủ tin để đưa vào báo cáo hội đồng.',
      body: 'Một nguồn dữ liệu duy nhất, có dấu vết chỉnh sửa và phân quyền — không còn ba phiên bản Excel cho cùng một quý.',
      points: [
        { h: 'Nhất quán giữa các kỳ', d: 'Cùng định nghĩa doanh thu, cùng cách ghi nhận, mọi tháng.' },
        { h: 'Truy vết mọi thay đổi', d: 'Ai sửa gì, lúc nào, trên chứng từ nào đều có nhật ký.' },
        { h: 'Xuất báo cáo theo chuẩn', d: 'Kết xuất cho kiểm toán và hội đồng quản trị không cần làm lại.' }
      ],
      foot: 'Dữ liệu sẵn sàng cho thẩm định tài chính mà không cần dự án dọn dữ liệu riêng.',
      screen: 'Chỉ số theo quý', colA: 'Chỉ số', colB: 'Quý trước', colC: 'Quý này',
      rows: [
        { a: 'Doanh thu', b: '83,9 tỷ', c: '94,3 tỷ' },
        { a: 'Biên lợi nhuận', b: '6,1%', c: '6,8%' },
        { a: 'Giá trị TB/đơn', b: '28,7 tr', c: '31,4 tr' },
        { a: 'Khách quay lại', b: '22%', c: '27%' }
      ],
      stats: [{ k: 'Tăng trưởng', v: '+12,4%' }, { k: 'Số đơn', v: '3.005' }, { k: 'Tỷ lệ thu', v: '19,8%' }] }
  ];

  var pricingDataEl = document.getElementById('pricing-data');
  var pricingData = pricingDataEl ? JSON.parse(pricingDataEl.textContent) : { plans: [], compare: [] };
  var PLANS = pricingData.plans;
  var COMPARE = pricingData.compare;

  var FAQS = [
    { q: 'Dữ liệu cũ trên Excel có chuyển sang được không?', a: 'Được. Đội triển khai nhận file khách hàng, tour, công nợ và nhà cung cấp hiện tại của bạn, chuẩn hoá rồi nạp vào hệ thống. Việc này thường mất 3–7 ngày làm việc và bạn vẫn dùng file cũ song song cho tới khi yên tâm.' },
    { q: 'Đội ngũ lớn tuổi, quen Excel thì học có khó không?', a: 'Mỗi vai trò chỉ thấy màn hình của mình, không thấy toàn bộ hệ thống. Buổi đào tạo cho một phòng ban thường gói trong 90 phút, và tuần đầu luôn có người trực hỗ trợ qua Zalo.' },
    { q: 'Công ty tôi có quy trình riêng, hệ thống có theo được không?', a: 'Quy trình duyệt báo giá, phân quyền, cơ cấu phòng ban và cách tính hoa hồng đều cấu hình được theo cách bạn đang làm. Chúng tôi rà quy trình hiện tại trước khi thiết lập, không bắt bạn đổi cách làm nghề.' },
    { q: 'Dữ liệu khách hàng có an toàn không?', a: 'Dữ liệu lưu tại trung tâm dữ liệu ở Việt Nam, sao lưu hằng ngày, phân quyền tới từng chức năng và có nhật ký truy cập. Bạn xuất toàn bộ dữ liệu của mình bất cứ lúc nào.' },
    { q: 'Đang dùng phần mềm kế toán khác thì sao?', a: 'M-Travel không thay phần mềm kế toán của bạn. Chúng tôi đồng bộ chứng từ thu chi và công nợ qua kết nối dữ liệu, để kế toán không phải nhập hai lần.' },
    { q: 'Nếu dùng thấy không hợp thì sao?', a: 'Bạn dừng bất cứ lúc nào, không ràng buộc hợp đồng dài hạn ở gói tháng. Toàn bộ dữ liệu được xuất trả cho bạn ở định dạng đọc được.' }
  ];

  /* ---------------- Tiện ích ---------------- */

  var $ = function (s) { return document.querySelector(s); };
  var esc = function (t) {
    return String(t).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  };

  /* ---------------- Tab vai trò ---------------- */

  var roleIndex = 0;

  function renderRoleTabs() {
    var host = $('#role-tabs');
    if (!host) return;
    host.innerHTML = ROLES.map(function (r, i) {
      return '<button type="button" class="role-tab' + (i === roleIndex ? ' is-active' : '') + '" data-role="' + i + '">' + esc(r.name) + '</button>';
    }).join('');
  }

  function renderRolePanel() {
    var host = $('#role-panel');
    if (!host) return;
    var r = ROLES[roleIndex];

    var points = r.points.map(function (p) {
      return '<div class="flex gap-3 items-start">' +
        '<span class="shrink-0 mt-1.5 w-[7px] h-[7px] rounded-full bg-brand-600"></span>' +
        '<div><div class="text-[15px] font-semibold">' + esc(p.h) + '</div>' +
        '<div class="mt-0.5 text-[14.5px] leading-normal text-muted-2">' + esc(p.d) + '</div></div></div>';
    }).join('');

    var rows = r.rows.map(function (row) {
      return '<div class="grid grid-cols-3 border-b border-line-soft text-[12.5px] lg:text-[13.5px]">' +
        '<div class="px-2.5 py-3 lg:px-3.5 font-medium">' + esc(row.a) + '</div>' +
        '<div class="px-2.5 py-3 lg:px-3.5 text-muted-2">' + esc(row.b) + '</div>' +
        '<div class="px-2.5 py-3 lg:px-3.5 text-right">' + esc(row.c) + '</div></div>';
    }).join('');

    var stats = r.stats.map(function (s) {
      return '<div class="border border-line-soft rounded-xl p-3.5 bg-[#FAFAFB]">' +
        '<div class="text-[12px] text-muted-3">' + esc(s.k) + '</div>' +
        '<div class="mt-1 text-[23px] font-bold tracking-tight">' + esc(s.v) + '</div></div>';
    }).join('');

    host.innerHTML =
      '<div class="bg-white border border-line rounded-[18px] p-6 lg:p-8 flex flex-col">' +
        '<h3 class="text-[22px] lg:text-[29px] leading-snug font-bold tracking-tight">' + esc(r.title) + '</h3>' +
        '<p class="mt-3.5 text-[16px] leading-relaxed text-muted">' + esc(r.body) + '</p>' +
        '<div class="mt-6 flex flex-col gap-3.5">' + points + '</div>' +
        '<div class="mt-auto pt-7 text-[14.5px] text-muted-3 border-t border-line-soft">' + esc(r.foot) + '</div>' +
      '</div>' +
      '<div class="bg-white border border-line rounded-[18px] p-5 flex flex-col gap-3.5">' +
        '<div class="flex items-center justify-between gap-3">' +
          '<div class="text-[14px] font-semibold">' + esc(r.screen) + '</div>' +
          '<div class="text-[12px] text-muted-4">M-Travel · bản demo</div></div>' +
        '<div class="border border-line-soft rounded-xl overflow-hidden">' +
          '<div class="grid grid-cols-3 bg-[#FAFAFB] border-b border-line-soft text-[11.5px] text-muted-3 tracking-wide">' +
            '<div class="px-2.5 py-2.5 lg:px-3.5">' + esc(r.colA) + '</div>' +
            '<div class="px-2.5 py-2.5 lg:px-3.5">' + esc(r.colB) + '</div>' +
            '<div class="px-2.5 py-2.5 lg:px-3.5 text-right">' + esc(r.colC) + '</div></div>' +
          rows +
        '</div>' +
        '<div class="grid grid-cols-3 gap-3">' + stats + '</div>' +
      '</div>';
  }

  /* ---------------- Bảng giá ---------------- */

  function renderPlans() {
    var host = $('#plans');
    if (!host) return;

    host.innerHTML = PLANS.map(function (p, idx) {
      var hi = idx === 1;
      var card = 'rounded-[18px] p-6 lg:p-[30px] transition hover:-translate-y-1.5 ' + (hi
        ? 'bg-ink text-white border border-ink shadow-[0_24px_50px_-28px_rgba(15,15,20,0.5)]'
        : 'bg-white text-ink border border-line');
      var mutedCls = hi ? 'text-[#A4A4B0]' : 'text-muted-2';
      var lineCls = hi ? 'border-night-line' : 'border-line-soft';
      var btn = 'flex justify-center mt-5 text-[15px] font-semibold px-5 py-3.5 rounded-full ' + (hi
        ? 'bg-brand-600 hover:bg-[#6E6EE8] text-white'
        : 'bg-[#F6F6F8] hover:bg-line text-ink border border-line-hard');
      var badge = p.badge
        ? '<span class="text-[11.5px] font-semibold tracking-wide px-2.5 py-1 rounded-full bg-brand-500 text-white">' + esc(p.badge) + '</span>'
        : '';
      var features = p.features.map(function (f) {
        return '<div class="flex gap-2.5 text-[14.5px] leading-normal">' +
          '<span class="shrink-0 text-brand-600">✓</span><span>' + esc(f) + '</span></div>';
      }).join('');

      return '<div class="' + card + '">' +
        '<div class="flex items-center justify-between gap-3"><div class="text-[16px] font-semibold">' + esc(p.name) + '</div>' + badge + '</div>' +
        '<div class="mt-2 min-h-[44px] text-[14.5px] leading-normal ' + mutedCls + '">' + esc(p.for) + '</div>' +
        '<div class="mt-4 flex items-baseline gap-2"><span class="text-[34px] lg:text-[42px] font-bold tracking-tight">' + esc(p.price) + '</span>' +
          '<span class="text-[14px] ' + mutedCls + '">' + esc(p.unit) + '</span></div>' +
        '<div class="mt-1 text-[13px] ' + mutedCls + '">' + esc(p.note) + '</div>' +
        '<a href="#cta" class="' + btn + '">' + esc(p.cta) + '</a>' +
        '<div class="mt-5 pt-5 border-t ' + lineCls + ' flex flex-col gap-2.5">' + features + '</div>' +
      '</div>';
    }).join('');

  }

  function renderCompare() {
    var host = $('#compare');
    if (!host) return;
    var headings = PLANS.map(function (p) {
      return '<div class="px-3 py-4 lg:px-5 text-center">' + esc(p.name) + '</div>';
    }).join('');
    var rows = COMPARE.map(function (c) {
      return '<div class="grid grid-cols-[1.4fr_.8fr_.8fr_.8fr] lg:grid-cols-[1.6fr_1fr_1fr_1fr] border-b border-line-soft text-[13px] lg:text-[14.5px]">' +
        '<div class="px-3 py-3.5 lg:px-5 lg:py-4 text-[#3A3A44]">' + esc(c.label) + '</div>' +
        '<div class="px-3 py-3.5 lg:px-5 lg:py-4 text-center text-muted-2">' + esc(c.a) + '</div>' +
        '<div class="px-3 py-3.5 lg:px-5 lg:py-4 text-center text-muted-2">' + esc(c.b) + '</div>' +
        '<div class="px-3 py-3.5 lg:px-5 lg:py-4 text-center text-muted-2">' + esc(c.c) + '</div></div>';
    }).join('');
    host.innerHTML =
      '<div class="grid grid-cols-[1.4fr_.8fr_.8fr_.8fr] lg:grid-cols-[1.6fr_1fr_1fr_1fr] bg-[#F6F6F8] border-b border-line text-[13px] lg:text-[14px] font-semibold">' +
        '<div class="px-3 py-4 lg:px-5">So sánh chi tiết</div>' + headings +
      '</div>' + rows;
  }

  /* ---------------- FAQ ---------------- */

  function renderFaqs() {
    var host = $('#faqs');
    if (!host) return;
    host.innerHTML = FAQS.map(function (f, i) {
      return '<div class="faq-item' + (i === 0 ? ' is-open' : '') + '">' +
        '<button type="button" class="faq-q">' + esc(f.q) + '<span class="faq-icon">+</span></button>' +
        '<div class="faq-a"' + (i === 0 ? '' : ' hidden') + '>' + esc(f.a) + '</div></div>';
    }).join('');
  }

  /* ---------------- Reveal khi cuộn ---------------- */

  function initReveal() {
    var els = document.querySelectorAll('.reveal');
    if (!('IntersectionObserver' in window)) {
      Array.prototype.forEach.call(els, function (el) { el.classList.add('is-in'); });
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (!e.isIntersecting) return;
        e.target.classList.add('is-in');
        io.unobserve(e.target);
      });
    }, { threshold: 0.12 });
    Array.prototype.forEach.call(els, function (el) { io.observe(el); });
  }

  /* ---------------- Sự kiện ---------------- */

  document.addEventListener('click', function (e) {
    var tab = e.target.closest('.role-tab');
    if (tab) { roleIndex = +tab.getAttribute('data-role'); renderRoleTabs(); renderRolePanel(); return; }

    if (e.target.closest('#nav-toggle')) {
      var menu = $('#nav-mobile');
      menu.classList.toggle('hidden');
      menu.classList.toggle('flex');
      return;
    }
    if (e.target.closest('#nav-mobile a')) {
      $('#nav-mobile').classList.add('hidden');
      $('#nav-mobile').classList.remove('flex');
    }

    var q = e.target.closest('.faq-q');
    if (q) {
      var item = q.parentElement;
      var wasOpen = item.classList.contains('is-open');
      Array.prototype.forEach.call(document.querySelectorAll('.faq-item'), function (it) {
        it.classList.remove('is-open');
        it.querySelector('.faq-a').hidden = true;
      });
      if (!wasOpen) { item.classList.add('is-open'); item.querySelector('.faq-a').hidden = false; }
    }
  });

  renderRoleTabs();
  renderRolePanel();
  renderPlans();
  renderCompare();
  renderFaqs();
  initReveal();
})();
