/* tk-ai.js — trợ lý AI: panel trượt phải, có ở MỌI trang.
   Nút mở nằm ở navbar (#tk-ai-launch) và chỉ hiện khi /tro-ly?handler=Status báo tính năng đang bật.
   Vỏ panel dùng offcanvas của Bootstrap có sẵn trong theme, không tự dựng lớp phủ. */
(function () {
  'use strict';

  var API = '/tro-ly';
  var MAX_LEN = 2000;

  var GOI_Y = [
    'Công ty đang thế nào?',
    'Khách nào đang nợ nhiều nhất?',
    'Doanh thu từng chi nhánh ra sao?',
    'Tỉ lệ chốt báo giá bao nhiêu?'
  ];

  var panel, log, input, sendBtn, busy = false;

  function esc(s) { return window.tk ? tk.escape(s) : String(s == null ? '' : s); }

  function money(n) { return (Number(n) || 0).toLocaleString('vi-VN'); }

  // Panel chỉ rộng 32rem mà số tiền ngành tour thường 10-11 chữ số: để nguyên thì bảng 7 cột phải
  // cuộn ngang mới đọc được cột đầu. Rút gọn theo đúng cách màn Tổng quan đang làm ("48,2 tỷ"),
  // số đầy đủ vẫn còn trong title để rê chuột kiểm chứng.
  function moneyShort(n) {
    var v = Number(n) || 0;
    var abs = Math.abs(v);
    if (abs >= 1e9) { return (v / 1e9).toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + ' tỷ'; }
    if (abs >= 1e6) { return (v / 1e6).toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + ' tr'; }
    return money(v);
  }

  // ---- Dựng vỏ panel một lần, gắn vào cuối body ----
  function build() {
    var el = document.createElement('div');
    el.className = 'offcanvas offcanvas-end tk-ai';
    el.id = 'tk-ai-panel';
    el.tabIndex = -1;
    el.innerHTML =
      '<div class="offcanvas-header border-bottom">' +
        '<h5 class="offcanvas-title mb-0"><i class="ti ti-sparkles me-2 text-primary"></i>Trợ lý</h5>' +
        '<button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Đóng"></button>' +
      '</div>' +
      '<div class="offcanvas-body d-flex flex-column p-0">' +
        '<div class="tk-ai-log flex-grow-1 overflow-auto p-4"></div>' +
        '<div class="border-top p-3">' +
          '<div class="d-flex gap-2 align-items-end">' +
            '<textarea class="form-control tk-ai-input" rows="1" maxlength="' + MAX_LEN + '" ' +
              'placeholder="Hỏi bằng tiếng Việt, ví dụ: tháng này thu được bao nhiêu?"></textarea>' +
            '<button type="button" class="btn btn-primary tk-ai-send" title="Gửi (Enter)">' +
              '<i class="ti ti-send"></i></button>' +
          '</div>' +
          '<div class="text-muted small mt-2">Trợ lý chỉ đọc số liệu, không sửa dữ liệu. Số liệu đã lọc theo quyền của bạn.</div>' +
        '</div>' +
      '</div>';
    document.body.appendChild(el);

    panel = el;
    log = el.querySelector('.tk-ai-log');
    input = el.querySelector('.tk-ai-input');
    sendBtn = el.querySelector('.tk-ai-send');

    sendBtn.addEventListener('click', send);

    input.addEventListener('keydown', function (e) {
      // Enter gửi, Shift+Enter xuống dòng — quy ước quen thuộc của mọi khung chat.
      if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); }
    });

    // Ô nhập tự cao dần theo nội dung, chặn trần để không nuốt hết panel.
    input.addEventListener('input', function () {
      input.style.height = 'auto';
      input.style.height = Math.min(input.scrollHeight, 140) + 'px';
    });

    el.addEventListener('shown.bs.offcanvas', function () { input.focus(); });

    empty();
  }

  // ---- Màn hình trống: gợi ý câu hỏi để người dùng biết trợ lý làm được gì ----
  function empty() {
    var html =
      '<div class="text-center text-muted py-4">' +
        '<i class="ti ti-sparkles ti-lg mb-2 d-block text-primary"></i>' +
        '<div class="mb-3">Hỏi tôi về số liệu kinh doanh của công ty.</div>' +
        '<div class="d-flex flex-column gap-2 align-items-stretch">';
    for (var i = 0; i < GOI_Y.length; i++) {
      html += '<button type="button" class="btn btn-sm btn-label-primary tk-ai-goiy">' + esc(GOI_Y[i]) + '</button>';
    }
    html += '</div></div>';
    log.innerHTML = html;

    log.querySelectorAll('.tk-ai-goiy').forEach(function (b) {
      b.addEventListener('click', function () { input.value = b.textContent; send(); });
    });
  }

  function bubble(role, html) {
    var wrap = document.createElement('div');
    wrap.className = 'mb-3 d-flex ' + (role === 'user' ? 'justify-content-end' : '');
    wrap.innerHTML = '<div class="tk-ai-msg tk-ai-' + role + '">' + html + '</div>';
    log.appendChild(wrap);
    log.scrollTop = log.scrollHeight;
    return wrap;
  }

  // ---- Vẽ một bảng do công cụ trả về ----
  // Bảng bọc trong .table-responsive: bảng nhiều cột mà tràn ra ngoài sẽ sinh thanh cuộn cho CẢ TRANG
  // chứ không phải cho riêng nó.
  function renderTable(t) {
    if (!t || !t.columns || !t.rows || !t.rows.length) { return ''; }

    var html = '<div class="table-responsive mt-2"><table class="table table-sm table-borderless mb-0"><thead><tr>';
    for (var c = 0; c < t.columns.length; c++) {
      var col = t.columns[c];
      var right = (col.type === 'money' || col.type === 'number' || col.type === 'percent');
      html += '<th class="text-nowrap' + (right ? ' text-end' : '') + '">' + esc(col.label) + '</th>';
    }
    html += '</tr></thead><tbody>';

    for (var r = 0; r < t.rows.length; r++) {
      html += '<tr>';
      for (var i = 0; i < t.columns.length; i++) {
        var type = t.columns[i].type;
        var v = t.rows[r][i];
        var cell = esc(v), attr = ' class="text-nowrap"';
        if (type === 'money') { cell = esc(moneyShort(v)); attr = ' class="text-end text-nowrap" title="' + esc(money(v)) + '"'; }
        else if (type === 'number') { cell = esc(money(v)); attr = ' class="text-end text-nowrap"'; }
        else if (type === 'percent') { attr = ' class="text-end text-nowrap"'; }
        html += '<td' + attr + '>' + cell + '</td>';
      }
      html += '</tr>';
    }
    return html + '</tbody></table></div>';
  }

  function renderAnswer(data) {
    // Giữ xuống dòng của model nhưng KHÔNG cho HTML nào lọt qua — nội dung này do model sinh ra.
    var html = '<div>' + esc(data.text).replace(/\n/g, '<br>') + '</div>';

    (data.blocks || []).forEach(function (b) {
      html += renderTable(b.data);
      if (b.linkUrl) {
        html += '<a href="' + esc(b.linkUrl) + '" class="btn btn-sm btn-label-primary mt-2">' +
                '<i class="ti ti-external-link me-1"></i>Mở màn hình đầy đủ</a>';
      }
    });

    return html;
  }

  function send() {
    if (busy) { return; }
    var q = (input.value || '').trim();
    if (!q) { return; }

    if (log.querySelector('.tk-ai-goiy')) { log.innerHTML = ''; }

    bubble('user', esc(q).replace(/\n/g, '<br>'));
    input.value = '';
    input.style.height = 'auto';

    busy = true;
    sendBtn.disabled = true;
    var thinking = bubble('bot', '<span class="tk-ai-dots"><span></span><span></span><span></span></span>');

    var fd = new FormData();
    fd.append('question', q);

    tk.post(API + '?handler=Ask', fd).then(function (res) {
      busy = false;
      sendBtn.disabled = false;
      input.focus();

      if (!res || !res.isSuccess) {
        thinking.querySelector('.tk-ai-msg').innerHTML =
          '<span class="text-danger">' + esc((res && res.message) || 'Trợ lý không trả lời được.') + '</span>';
        return;
      }
      thinking.querySelector('.tk-ai-msg').innerHTML = renderAnswer(res.data || {});
      log.scrollTop = log.scrollHeight;
    });
  }

  function open() {
    if (!panel) { build(); }
    window.bootstrap.Offcanvas.getOrCreateInstance(panel).show();
  }

  // ---- Khởi động: chỉ bày nút ra khi máy chủ báo tính năng đang bật ----
  function init() {
    var launch = document.getElementById('tk-ai-launch');
    if (!launch || !window.tk || !window.bootstrap) { return; }

    fetch(API + '?handler=Status', { headers: { 'X-Requested-With': 'fetch' } })
      .then(function (r) { return r.ok ? r.json() : { available: false }; })
      .catch(function () { return { available: false }; })
      .then(function (s) {
        if (!s || !s.available) { return; }
        launch.classList.remove('d-none');
        launch.addEventListener('click', open);
        document.addEventListener('keydown', function (e) {
          if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) { e.preventDefault(); open(); }
        });
      });
  }

  if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', init); }
  else { init(); }
})();

/* Thẻ trợ lý AI trên một bản ghi (gộp trong tk-ai.js) — chấm điểm, tóm tắt, soạn tin.
   Mount: <div data-ai-review data-entity="Lead" data-entity-id="..."></div>

   Kết quả ĐƯỢC LƯU, và mỗi lần bấm là một dòng mới chứ không ghi đè. Bản trước không lưu gì, nên mở
   lại hồ sơ hôm sau là thẻ trống trơn và muốn xem lại phải chạy lại — mất 15-20 giây cùng một lượt
   gọi model có tính phí, mà vẫn không so được điểm hôm nay với điểm tuần trước.
   Nguy cơ của việc lưu là người đọc tưởng điểm cũ là hiện trạng; chặn bằng cách LUÔN hiện thời điểm
   chấm và người đã chấm ngay cạnh kết quả. */
(function () {
  'use strict';

  var API = '/danh-gia-ai';
  var available = null;   // null = chưa hỏi máy chủ; sau đó là { review, summary, draft }

  function esc(s) { return window.tk ? tk.escape(s) : String(s == null ? '' : s); }

  function bandClass(band) {
    if (band === 'Nóng') { return 'bg-label-danger'; }
    if (band === 'Ấm') { return 'bg-label-warning'; }
    return 'bg-label-secondary';
  }

  function list(title, items, icon) {
    if (!items || !items.length) { return ''; }
    var html = '<div class="mt-3"><div class="fw-medium mb-1"><i class="ti ' + icon + ' me-1"></i>' + esc(title) + '</div><ul class="mb-0 ps-3">';
    for (var i = 0; i < items.length; i++) { html += '<li>' + esc(items[i]) + '</li>'; }
    return html + '</ul></div>';
  }

  // Bảng chi tiết từng tiêu chí: điểm tổng chỉ là con số, cái người dùng cần là MẤT ĐIỂM Ở ĐÂU.
  // Không có bảng này thì nhận định của AI là một lời phán không cãi được, và cũng không sửa được.
  function breakdown(items) {
    if (!items || !items.length) { return ''; }

    // Bố cục DỌC (không phải bảng 4 cột): panel AI hẹp, nhồi giải thích vào một ô ~30% thì chữ vỡ
    // xuống dòng liên tục. Mỗi tiêu chí: hàng [nhãn·trọng số  —  điểm] + thanh tiến độ full + ghi
    // chú FULL-WIDTH bên dưới → đọc thoáng, không bị bóp.
    var html = '<div class="mt-3">';
    for (var i = 0; i < items.length; i++) {
      var c = items[i];
      var pct = Math.max(0, Math.min(100, Number(c.score) || 0));
      html +=
        '<div class="' + (i ? 'pt-2 mt-2 border-top' : '') + '">' +
          '<div class="d-flex justify-content-between align-items-baseline gap-2 mb-1">' +
            '<span class="small fw-medium">' + esc(c.label) +
              '<span class="text-muted fw-normal"> · ' + esc(c.weight) + '%</span></span>' +
            '<span class="small fw-semibold flex-shrink-0">' + pct + '</span>' +
          '</div>' +
          '<div class="progress" style="height:.375rem"><div class="progress-bar" style="width:' + pct + '%"></div></div>' +
          (c.note ? '<div class="text-muted small mt-1">' + esc(c.note) + '</div>' : '') +
        '</div>';
    }
    return html + '</div>';
  }

  function render(box, d) {
    box.innerHTML =
      '<div class="d-flex align-items-center gap-3 mb-2">' +
        '<div class="tk-ai-score">' + esc(d.score) + '</div>' +
        '<div><span class="badge ' + bandClass(d.band) + '">' + esc(d.band) + '</span>' +
        '<div class="text-muted small mt-1">điểm ưu tiên trên thang 100</div></div>' +
      '</div>' +
      '<div>' + esc(d.summary) + '</div>' +
      breakdown(d.criteria) +
      list('Rủi ro / còn thiếu', d.risks, 'ti-alert-triangle') +
      list('Nên làm tiếp', d.nextActions, 'ti-arrow-right') +
      '<div class="text-muted small mt-3">Điểm tổng tính theo trọng số các tiêu chí trong cấu hình, ' +
      'không phải do AI tự phán. Đây là gợi ý — bạn tự quyết định.</div>';
  }

  /**
   * Dòng "chấm lúc nào, ai chấm" đặt ngay trên kết quả cũ.
   *
   * Bắt buộc phải có khi đã lưu lịch sử: không có nó thì một điểm số chấm từ tháng trước trông y hệt
   * điểm vừa chấm xong, và người đọc ra quyết định trên hiện trạng đã cũ mà không hề biết.
   */
  function metaLine(item, nhan) {
    var t = new Date(item.createdAt);
    var luc = isNaN(t) ? '' : t.toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    return '<div class="d-flex align-items-center gap-2 mb-2 text-muted small">' +
             '<i class="ti ti-history"></i><span>' + esc(nhan) + ' lúc ' + esc(luc) +
             (item.userName ? ' · ' + esc(item.userName) : '') + '</span>' +
           '</div>';
  }

  var NHAN = { Review: 'Đã chấm', Summary: 'Đã tóm tắt', Draft: 'Đã soạn tin' };

  /**
   * Đọc một tiêu chí bất kể hoa/thường.
   *
   * Bản lưu TRƯỚC bản vá được ghi bằng JsonSerializer không kèm options nên ra PascalCase
   * (`Label`/`Weight`/`Score`/`Note`), trong khi chỗ đọc dùng camelCase → mọi bản chấm cũ hiện
   * nhãn trống và điểm 0. Máy chủ đã ghi đúng từ nay, nhưng những dòng ĐÃ nằm trong DB thì không
   * tự sửa được, nên chỗ đọc phải nhận cả hai kiểu.
   */
  function tieuChi(c) {
    if (!c) { return {}; }
    return {
      label:  c.label  != null ? c.label  : c.Label,
      weight: c.weight != null ? c.weight : c.Weight,
      score:  c.score  != null ? c.score  : c.Score,
      note:   c.note   != null ? c.note   : c.Note
    };
  }

  /** Dựng lại kết quả đã lưu (không gọi model). */
  function renderSaved(out, item) {
    var meta = metaLine(item, NHAN[item.kind] || 'Đã chạy');
    if (item.kind === 'Review') {
      var d = { score: item.score, band: item.band, summary: item.summary, criteria: [], risks: [], nextActions: [] };
      try {
        var ct = JSON.parse(item.detailJson || '{}');
        d.criteria = (ct.criteria || []).map(tieuChi);
        d.risks = ct.risks || [];
        d.nextActions = ct.nextActions || [];
      } catch (e) { /* lịch sử cũ có thể thiếu chi tiết — vẫn hiện được điểm và nhận định */ }
      render(out, d);
    } else {
      renderText(out, item.text, item.kind === 'Draft');
    }
    out.insertAdjacentHTML('afterbegin', meta);
  }

  function mount(el) {
    var entity = el.getAttribute('data-entity');
    if (!entity || !el.getAttribute('data-entity-id')) { return; }

    // Ba việc AI làm được trên một bản ghi, gộp vào MỘT thẻ. Rải ba nút ở ba chỗ trên màn hình thì
    // người dùng phải nhớ cái nào ở đâu; gộp lại thì chỉ cần nhớ "chỗ này là AI".
    var actions = [
      { key: 'Review',  label: 'Chấm điểm', lai: 'Chấm lại',     icon: 'ti-target',  wait: 'AI đang đọc hồ sơ và luồng trao đổi, thường mất khoảng 20 giây…', moi: 'Chưa chấm điểm lần nào. AI sẽ đọc hồ sơ và luồng trao đổi rồi cho điểm ưu tiên kèm lý do.' },
      { key: 'Summary', label: 'Tóm tắt',   lai: 'Tóm tắt lại',  icon: 'ti-list',    wait: 'AI đang đọc diễn biến…', moi: 'Chưa có bản tóm tắt nào. AI sẽ rút gọn diễn biến của bản ghi này.' },
      { key: 'Draft',   label: 'Soạn tin',  lai: 'Soạn lại',     icon: 'ti-message', wait: 'AI đang soạn tin nhắn…', moi: 'Chưa soạn tin nào. AI sẽ viết sẵn một tin nhắn gửi khách để bạn sửa lại rồi gửi.' }
    ].filter(function (a) { return available[a.key.toLowerCase()] !== false; });

    if (!actions.length) { return; }

    // MỖI loại một tab, mỗi tab giữ kết quả riêng.
    // Bản trước chỉ có MỘT khung: bấm "Tóm tắt" là ghi đè lên bản chấm điểm đang xem, đồng thời nút
    // lịch sử lật sang loại vừa chạy — bản chấm điểm không còn đường nào quay lại từ giao diện, muốn
    // xem phải chấm lại và trả thêm một lượt model. Trong khi handler Latest VỐN đã trả về cả ba
    // loại, giao diện chỉ lấy cái mới nhất rồi vứt hai cái kia đi.
    var tabs = actions.map(function (a, i) {
      return '<li class="nav-item">' +
             '<button type="button" class="nav-link' + (i ? '' : ' active') + '" data-tab="' + a.key + '">' +
             '<i class="ti ' + a.icon + ' me-1"></i>' + esc(a.label) + '</button></li>';
    }).join('');

    el.innerHTML =
      '<div class="card tk-ai-review-card">' +
        '<div class="card-body">' +
          '<h6 class="mb-3"><i class="ti ti-sparkles me-2 text-primary"></i>Trợ lý AI</h6>' +
          '<ul class="nav nav-tabs tk-ai-tabs" role="tablist">' + tabs + '</ul>' +
          '<div data-role="out" class="mt-3"></div>' +
          '<div data-role="history" class="mt-3 d-none"></div>' +
        '</div>' +
      '</div>';

    var out = el.querySelector('[data-role="out"]');
    var lichSu = el.querySelector('[data-role="history"]');
    var tabHienTai = actions[0].key;
    var daLuu = {};        // kết quả ĐÃ LƯU của từng loại (từ handler Latest)
    var ketQuaMoi = {};    // kết quả vừa chạy trong phiên này, giữ lại khi đổi tab
    var goc = null;        // kết quả của BẢN GHI TIỀN THÂN (khách hàng ← khách tiềm năng)

    el.querySelectorAll('[data-tab]').forEach(function (b) {
      b.addEventListener('click', function () { veTab(b.dataset.tab); });
    });

    veTab(tabHienTai);

    // Hiện ngay kết quả đã lưu. Đây là toàn bộ lý do lưu: mở hồ sơ ra là thấy, không phải bấm lại
    // và trả tiền model thêm một lượt cho một câu trả lời đã có.
    fetch(API + '?handler=Latest&entity=' + encodeURIComponent(entity) +
          '&id=' + encodeURIComponent(el.getAttribute('data-entity-id')))
      .then(function (r) { return r.ok ? r.json() : null; })
      .catch(function () { return null; })
      .then(function (res) {
        var data = (res && res.isSuccess && res.data) || {};
        var items = data.items || [];

        // Gán TRƯỚC và KHÔNG thoát sớm khi rỗng: bản ghi này có thể chưa chấm lần nào mà bản ghi
        // tiền thân thì có (khách vừa chuyển từ khách tiềm năng đã được chấm) — thoát sớm là đúng
        // ca đó mất sạch phần lịch sử cần hiện nhất.
        goc = data.goc || null;
        items.forEach(function (it) { daLuu[it.kind] = it; });

        // Mở đúng tab của việc người dùng làm gần nhất — nhưng các tab kia vẫn còn nguyên nội dung.
        items.sort(function (a, b) { return new Date(b.createdAt) - new Date(a.createdAt); });
        var uuTien = items.filter(function (it) {
          return actions.some(function (a) { return a.key === it.kind; });
        })[0];
        veTab(uuTien ? uuTien.kind : tabHienTai);
      });

    /** Vẽ một tab: ưu tiên kết quả vừa chạy, rồi tới bản đã lưu, cuối cùng là lời mời chạy. */
    function veTab(kind) {
      tabHienTai = kind;
      el.querySelectorAll('[data-tab]').forEach(function (b) {
        b.classList.toggle('active', b.dataset.tab === kind);
      });

      var a = actions.filter(function (x) { return x.key === kind; })[0];
      if (!a) { return; }

      var moi = ketQuaMoi[kind];
      var luu = daLuu[kind];

      if (moi) {
        if (kind === 'Review') { render(out, moi); }
        else { renderText(out, moi.text, kind === 'Draft'); }
      } else if (luu) {
        renderSaved(out, luu);
      } else {
        out.innerHTML = '<div class="text-muted small">' + esc(a.moi) + '</div>';
      }

      out.insertAdjacentHTML('beforeend',
        '<div class="mt-3"><button type="button" class="btn btn-sm btn-label-primary" data-act="' + kind + '">' +
        '<i class="ti ' + a.icon + ' me-1"></i>' + esc(moi || luu ? a.lai : a.label) + '</button></div>');
      out.querySelector('[data-act]').addEventListener('click', function () { chay(a); });

      // Kết quả của BẢN GHI TIỀN THÂN (khách hàng ← khách tiềm năng). Chuyển đổi sinh bản ghi mới
      // nên lịch sử ở lại bên gốc; hiện lại ở đây để nó không đứt đúng lúc khách thành khách hàng.
      // Ghi rõ "từ <gốc>" và kèm đường mở hồ sơ gốc — người đọc phải biết mình đang xem đánh giá
      // của GIAI ĐOẠN TRƯỚC, không phải đánh giá cho bản ghi đang mở.
      var itemGoc = goc && (goc.items || []).filter(function (it) { return it.kind === kind; })[0];
      if (itemGoc) {
        out.insertAdjacentHTML('beforeend',
          '<div class="mt-3 pt-3 border-top">' +
            '<div class="small text-muted mb-2">' +
              '<i class="ti ti-corner-down-right me-1"></i>Từ ' + esc(goc.label) + ' · ' +
              '<a href="' + esc(goc.url) + '">mở hồ sơ gốc</a>' +
            '</div><div data-role="goc-out"></div>' +
          '</div>');
        renderSaved(out.querySelector('[data-role="goc-out"]'), itemGoc);
      }

      capNhatNutLichSu();
    }

    function chay(a) {
      out.innerHTML =
        '<div class="d-flex align-items-center gap-2">' +
          '<span class="tk-ai-dots"><span></span><span></span><span></span></span>' +
          '<span class="text-muted small">' + esc(a.wait) + '</span>' +
        '</div>';

      var fd = new FormData();
      fd.append('entity', entity);
      fd.append('id', el.getAttribute('data-entity-id'));   // đọc lại: id đổi khi mở bản ghi khác

      tk.post(API + '?handler=' + a.key, fd).then(function (res) {
        if (!res || !res.isSuccess) {
          out.innerHTML = '<span class="text-danger">' + esc((res && res.message) || 'Chưa làm được.') + '</span>';
          out.insertAdjacentHTML('beforeend',
            '<div class="mt-3"><button type="button" class="btn btn-sm btn-label-primary" data-act="' + a.key + '">' +
            '<i class="ti ' + a.icon + ' me-1"></i>Thử lại</button></div>');
          out.querySelector('[data-act]').addEventListener('click', function () { chay(a); });
          return;
        }
        ketQuaMoi[a.key] = res.data || {};
        veTab(a.key);
      });
    }

    function capNhatNutLichSu() {
      if (!tabHienTai || !(ketQuaMoi[tabHienTai] || daLuu[tabHienTai])) {
        lichSu.classList.add('d-none');
        return;
      }
      lichSu.classList.remove('d-none');
      lichSu.innerHTML =
        '<button type="button" class="btn btn-sm btn-label-secondary" data-role="mo-lich-su">' +
        '<i class="ti ti-history me-1"></i>Các lần ' + esc((NHAN[tabHienTai] || 'đã chạy').toLowerCase()) + ' trước</button>';
      lichSu.querySelector('[data-role="mo-lich-su"]').addEventListener('click', moLichSu);
    }

    function moLichSu() {
      lichSu.innerHTML = '<div class="text-muted small">Đang tải lịch sử…</div>';
      fetch(API + '?handler=History&entity=' + encodeURIComponent(entity) +
            '&id=' + encodeURIComponent(el.getAttribute('data-entity-id')) +
            '&kind=' + encodeURIComponent(tabHienTai))
        .then(function (r) { return r.ok ? r.json() : null; })
        .catch(function () { return null; })
        .then(function (res) {
          var items = (res && res.isSuccess && res.data && res.data.items) || [];
          if (items.length <= 1) {
            lichSu.innerHTML = '<div class="text-muted small">Chưa có lần nào trước đó.</div>';
            return;
          }

          // Bỏ dòng đầu: nó chính là kết quả đang hiện ở trên, lặp lại chỉ gây rối.
          var html = '<div class="fw-medium small mb-2">Các lần trước</div><ul class="list-unstyled mb-0">';
          for (var i = 1; i < items.length; i++) {
            var it = items[i];
            var t = new Date(it.createdAt);
            var luc = isNaN(t) ? '' : t.toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
            html += '<li class="d-flex align-items-center gap-2 py-1 border-top">' +
                      (it.kind === 'Review'
                        ? '<span class="badge ' + bandClass(it.band) + '">' + esc(it.score) + '</span>'
                        : '<i class="ti ti-file-text text-muted"></i>') +
                      '<span class="small text-muted">' + esc(luc) + (it.userName ? ' · ' + esc(it.userName) : '') + '</span>' +
                      (it.kind !== 'Review' && it.text
                        ? '<span class="small text-truncate ms-1" style="max-width:22rem">' + esc(it.text) + '</span>'
                        : '') +
                    '</li>';
          }
          lichSu.innerHTML = html + '</ul>';
        });
    }

  }

  // Văn bản do model sinh ra: escape hết, chỉ giữ xuống dòng. Bản nháp tin nhắn kèm nút chép để
  // người dùng dán thẳng sang Zalo mà không phải bôi đen bằng tay.
  function renderText(box, text, copyable) {
    if (!text) { box.innerHTML = '<span class="text-danger">Không có nội dung.</span>'; return; }

    box.innerHTML =
      '<div class="tk-ai-text">' + esc(text).replace(/\n/g, '<br>') + '</div>' +
      (copyable
        ? '<button type="button" class="btn btn-sm btn-label-secondary mt-2" data-role="copy">' +
          '<i class="ti ti-copy me-1"></i>Chép nội dung</button>'
        : '');

    var copy = box.querySelector('[data-role="copy"]');
    if (copy) {
      copy.addEventListener('click', function () {
        navigator.clipboard.writeText(text).then(function () { tk.toast('Đã chép.'); });
      });
    }
  }

  function init() {
    var nodes = document.querySelectorAll('[data-ai-review]');
    if (!nodes.length || !window.tk) { return; }

    // Hỏi máy chủ MỘT lần: tính năng tắt thì không bày ra một cái nút bấm vào chỉ báo lỗi.
    if (available === null) {
      fetch(API + '?handler=Status')
        .then(function (r) { return r.ok ? r.json() : { available: false }; })
        .catch(function () { return { available: false }; })
        .then(function (s) {
          available = (s && s.available) ? s : false;
          if (available) { nodes.forEach(mount); }
        });
    } else if (available) {
      nodes.forEach(mount);
    }
  }

  // Màn Cơ hội dựng offcanvas động nên mount lại được khi có nội dung mới.
  window.tkAiReview = { mount: mount, init: init };

  if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', init); }
  else { init(); }
})();
