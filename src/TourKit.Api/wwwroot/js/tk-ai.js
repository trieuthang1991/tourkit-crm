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
