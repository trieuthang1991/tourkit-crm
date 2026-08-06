/* tk-grid.js — lưới danh sách CHUẨN của TourKit (Tabulator, phân trang từ server).
   Tách ra từ màn "Data khách hàng" để mọi màn danh sách dùng CHUNG một hành vi:
     · phân trang/lọc/sắp xếp đẩy hết xuống server (?handler=Data)
     · ô chọn nhiều dòng + thanh tác vụ hàng loạt
     · menu hành động (chuột phải trên dòng + nút ⋮ cuối dòng)
     · bảng luôn gọn trong 1 màn (chỉ 1 thanh cuộn, nằm trong bảng)
   Giao diện đi kèm ở css/tk-grid.css. Dùng: tk.grid('#grid-x', { columns, actions, ... }).

   Trang chỉ khai báo CỘT DỮ LIỆU — cột chọn và cột ⋮ do đây tự thêm. */
(function () {
  'use strict';
  var tk = window.tk || (window.tk = {});

  var esc = tk.escape || function (s) {
    return (s == null ? '' : String(s)).replace(/[&<>"]/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c];
    });
  };
  var money = tk.money || function (n) { return (Number(n) || 0).toLocaleString('vi-VN'); };

  // ===== Bộ dựng ô dùng chung (giữ mọi màn cùng một ngôn ngữ hình ảnh) =====
  var g = {};
  g.esc = esc;
  g.money = money;

  // Ô 2 dòng CHUẨN HOÁ — main đậm + sub mờ.
  g.stack = function (main, sub) {
    var m = '<div class="tk-cell-main" title="' + esc(main || '') + '">' + esc(main || '—') + '</div>';
    var s = sub ? '<div class="tk-cell-sub" title="' + esc(sub) + '">' + esc(sub) + '</div>' : '';
    return '<div class="tk-cell">' + m + s + '</div>';
  };

  // Bảng màu nhạt dùng CHUNG cho avatar + badge (liền mạch với dải thống kê).
  var TONES = ['primary', 'info', 'success', 'warning', 'danger', 'secondary'];
  g.toneOf = function (text) {
    var s = String(text || ''), h = 0;
    for (var i = 0; i < s.length; i++) { h = (h * 31 + s.charCodeAt(i)) >>> 0; }
    return TONES[h % TONES.length];
  };
  // Chữ cái đầu của (tối đa) 2 từ cuối — "Lê Đức Vân" → "ĐV".
  g.initials = function (name) {
    var w = String(name || '').trim().split(/\s+/).filter(Boolean);
    if (!w.length) { return '?'; }
    return (w.length === 1 ? w[0].slice(0, 2) : w[w.length - 2][0] + w[w.length - 1][0]).toUpperCase();
  };
  g.avatar = function (name, small) {
    return '<span class="tk-av ' + (small ? 'tk-av-sm ' : '') + 'bg-label-' + g.toneOf(name) + '">' + esc(g.initials(name)) + '</span>';
  };
  g.icLine = function (icon, text, cls) {
    if (!text) { return ''; }
    return '<div class="tk-ic-line ' + (cls || '') + '"><i class="' + icon + '"></i>' +
      '<span class="text-truncate" title="' + esc(text) + '">' + esc(text) + '</span></div>';
  };
  g.chip = function (text) {
    return '<span class="tk-chip-sm bg-label-' + g.toneOf(text) + '" title="' + esc(text) + '">' + esc(text) + '</span>';
  };
  // Ô tiền: số đậm bên phải + ghi chú mờ bên dưới.
  g.moneyCell = function (value, sub) {
    return '<div class="tk-cell tk-cell-right"><div class="tk-cell-main ' + ((value || 0) > 0 ? 'text-success' : '') + '">' +
      money(value) + '</div>' + (sub ? '<div class="tk-cell-sub">' + esc(sub) + '</div>' : '') + '</div>';
  };
  tk.g = g;

  /* tk.grid(selector, opts)
     opts:
       url          '?handler=Data'    — handler Razor (auth cookie; KHÔNG gọi /api/v1 vì API dùng JWT → 401)
       filters      ['status', …]      — đọc giá trị từ #f-<key>, gửi kèm mỗi lần tải
       columns      [...]              — CHỈ cột dữ liệu
       actions      function(row)      — trả danh sách mục menu hành động (Tabulator menu items)
       pageSize     20
       selectable   true               — cột chọn + thanh tác vụ hàng loạt (#bulkbar)
       totalLabel   'Tổng'             — nhãn ở dòng tổng (topCalc) tại cột chọn
       exportName   'danh-sach.csv'    — tên file khi xuất các dòng đã chọn
       bulkDeleteUrl '?handler=BulkDelete'
       onBulkDeleted function()        — chạy sau khi xoá hàng loạt xong
       wireFilters  true               — tự nối thanh lọc chuẩn (#btn-search, #f-q, #btn-reset, #btn-adv, .tk-chip, #type-tabs, #btn-export)
       chipGroups   ['segment', …]     — các nhóm chip LOẠI TRỪ nhau
       tabular      {...}              — tuỳ chọn Tabulator bổ sung (ghi đè)
     trả về: { table, reload, filters } */
  tk.grid = function (selector, opts) {
    opts = opts || {};
    var el = document.querySelector(selector);
    if (!el) { return null; }
    el.classList.add('tk-grid');

    var filterKeys = opts.filters || [];
    var selectable = opts.selectable !== false;

    function val(id) { var e = document.getElementById(id); return e ? String(e.value || '').trim() : ''; }

    function collectFilters() {
      var d = {};
      var q = val('f-q'); if (q) { d.q = q; }
      filterKeys.forEach(function (k) { var v = val('f-' + k); if (v) { d[k] = v; } });
      return d;
    }

    var actionsFor = opts.actions || function () { return []; };
    function rowActionMenu(e, row) { return actionsFor(row.getData()); }
    function cellActionMenu(e, cell) { return actionsFor(cell.getRow().getData()); }

    var columns = [];
    if (selectable) {
      columns.push({
        // Ô chọn để làm tác vụ hàng loạt; tiêu đề là ô chọn-tất-cả của trang.
        title: '', field: '__sel', width: 44, hozAlign: 'center', headerHozAlign: 'center',
        titleFormatter: 'rowSelection', formatter: 'rowSelection',
        headerSort: false, cellClick: function (e) { e.stopPropagation(); }
      });
    }
    var dataCols = (opts.columns || []).slice();
    // Nhãn "Tổng" của dòng tổng đặt ở cột DỮ LIỆU đầu tiên (cột ô chọn chỉ rộng 44px, chữ bị cắt).
    // Chỉ thêm khi màn THỰC SỰ có cột cộng tổng — nếu không sẽ thừa một dải trống chỉ ghi "Tổng".
    var hasCalc = dataCols.some(function (c) { return c && c.topCalc; });
    if (hasCalc && !dataCols[0].topCalc) {
      dataCols[0] = Object.assign({}, dataCols[0], {
        topCalc: function () { return ''; },
        topCalcFormatter: function () { return '<span class="fw-semibold text-muted">' + esc(opts.totalLabel || 'Tổng') + '</span>'; }
      });
    }
    columns = columns.concat(dataCols);
    if (opts.actions) {
      columns.push({
        title: '', field: '__act', width: 56, hozAlign: 'center', headerSort: false,
        clickMenu: cellActionMenu,
        formatter: function () {
          return '<button type="button" class="btn btn-icon btn-sm" title="Hành động"><i class="ti ti-dots-vertical"></i></button>';
        }
      });
    }

    var config = {
      // Toàn bộ chữ của lưới phải là TIẾNG VIỆT (quy ước UI của repo), không để "Showing 1-20 of…".
      locale: 'vi',
      langs: {
        vi: {
          pagination: {
            page_size: 'Số dòng', page_title: 'Tới trang',
            first: 'Đầu', first_title: 'Trang đầu',
            last: 'Cuối', last_title: 'Trang cuối',
            prev: 'Trước', prev_title: 'Trang trước',
            next: 'Sau', next_title: 'Trang sau',
            all: 'Tất cả',
            counter: { showing: 'Hiện', of: 'trên', rows: 'dòng', pages: 'trang' }
          },
          data: { loading: 'Đang tải…', error: 'Lỗi tải dữ liệu' }
        }
      },
      layout: 'fitColumns',
      // Chiều cao tính theo màn hình (fitHeight bên dưới) → bảng LUÔN gọn trong 1 màn,
      // cuộn diễn ra NGAY TRONG bảng nên chỉ có MỘT thanh cuộn, không cuộn trang.
      height: 420,
      placeholder: 'Không có dữ liệu',
      ajaxURL: opts.url || '?handler=Data',
      ajaxParams: collectFilters,
      pagination: true,
      paginationMode: 'remote',
      paginationSize: opts.pageSize || 20,
      paginationSizeSelector: [20, 50, 100],
      paginationCounter: 'rows',
      // Handler cũ của repo trả contract DataTables (draw/recordsTotal/recordsFiltered/data).
      // Tự suy ra last_page ở đây → chuyển một màn sang lưới mới KHÔNG phải sửa handler.
      ajaxResponse: function (url, params, response) {
        if (response && response.last_page == null && response.recordsFiltered != null) {
          var size = Number(params && params.size) || opts.pageSize || 20;
          response.last_page = Math.max(1, Math.ceil(response.recordsFiltered / size));
          response.last_row = response.recordsFiltered;
        }
        // Màn nào trả kèm số liệu riêng (stats, pageSum…) thì tự đọc ở đây để cập nhật KPI.
        if (opts.onData) { opts.onData(response); }
        return response;
      },
      columnDefaults: { headerSort: false, resizable: true, vertAlign: 'middle' },
      columns: columns
    };
    if (opts.actions) { config.rowContextMenu = rowActionMenu; }
    if (opts.onRowClick) { el.classList.add('tk-grid-clickable'); }
    if (selectable) {
      // KHÔNG dùng selectableRowsRangeMode:'click' — chế độ đó coi mỗi click là chọn một VÙNG mới
      // nên bấm ô thứ hai lại bỏ ô thứ nhất (chỉ chọn được 1).
      config.selectableRows = true;
    }
    Object.keys(opts.tabular || {}).forEach(function (k) { config[k] = opts.tabular[k]; });

    var table = new Tabulator(selector, config);

    // Tabulator 6 BỎ kiểu khai báo callback trong options (rowClick: fn không còn chạy) —
    // phải đăng ký qua bộ sự kiện. Đây là chỗ hay sai khi chuyển từ tài liệu bản 4/5.
    if (opts.onRowClick) {
      table.on('rowClick', function (e, row) {
        // Bấm vào ô chọn, nút ⋮ hay bất kỳ nút/link nào trong dòng thì để phần đó xử lý,
        // đừng mở luôn chi tiết — nếu không chọn một dòng cũng bật popup.
        if (e.target.closest('input, button, a, [tabulator-field="__sel"], [tabulator-field="__act"]')) { return; }
        // Dòng TỔNG (topCalc) cũng là một "row" của Tabulator — bấm vào nó mà mở chi tiết thì
        // popup hiện dữ liệu rỗng kèm số tiền là tổng cả trang.
        if (row.getElement().classList.contains('tabulator-calcs')) { return; }
        opts.onRowClick(row.getData(), row);
      });
    }

    function reload() { table.setData(); }
    // Form offcanvas (tk.form / customer-form.js) gọi hook này sau khi lưu → nạp lại lưới, không tải lại cả trang.
    window.tkGridReload = reload;

    wireBulkBar(table, reload, opts);
    fitHeight(el, table);
    if (opts.wireFilters !== false) { wireFilterBar(reload, filterKeys, collectFilters, opts); }

    return { table: table, reload: reload, filters: collectFilters };
  };

  // ===== Thanh tác vụ hàng loạt (#bulkbar) — chỉ hoạt động nếu trang có sẵn khối này =====
  function wireBulkBar(table, reload, opts) {
    var bulkbar = document.getElementById('bulkbar');
    if (!bulkbar) { return; }
    table.on('rowSelectionChanged', function (data) {
      var c = document.getElementById('bulk-count');
      if (c) { c.textContent = data.length; }
      bulkbar.classList.toggle('d-none', data.length === 0);
      bulkbar.classList.toggle('d-flex', data.length > 0);
    });
    on('bulk-clear', function () { table.deselectRow(); });
    // Xuất riêng các dòng đã chọn (dữ liệu đang có sẵn ở client → tải ngay, không gọi server).
    on('bulk-export', function () { table.download('csv', opts.exportName || 'danh-sach.csv', {}, 'selected'); });
    on('bulk-delete', function () {
      var sel = table.getSelectedData();
      if (!sel.length) { return; }
      var go = function () {
        // Gọi handler Razor (auth bằng cookie) — KHÔNG gọi /api/v1 vì API dùng JWT, sẽ 401.
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var fd = new FormData();
        sel.forEach(function (d) { fd.append('ids', d.id); });
        fetch(opts.bulkDeleteUrl || '?handler=BulkDelete', {
          method: 'POST',
          headers: token ? { 'RequestVerificationToken': token.value } : {},
          body: fd
        }).then(function (r) { return r.json(); }).then(function (res) {
          table.deselectRow();
          reload();
          if (res && res.message) {
            if (res.isSuccess && tk.toast) { tk.toast(res.message); }
            else if (!res.isSuccess && tk.error) { tk.error(res.message); }
          }
          if (opts.onBulkDeleted) { opts.onBulkDeleted(res); }
        });
      };
      if (tk.confirmDelete) {
        tk.confirmDelete({ title: 'Xoá ' + sel.length + ' mục đã chọn?' }).then(function (ok) { if (ok) { go(); } });
      } else if (window.confirm('Xoá ' + sel.length + ' mục đã chọn?')) { go(); }
    });
  }

  // ===== Bảng luôn nằm gọn trong 1 màn =====
  // Cao = phần viewport còn lại tính từ đỉnh bảng. Dùng toạ độ TÀI LIỆU (rect.top + scrollY)
  // để giá trị không đổi theo vị trí cuộn, nhờ vậy setHeight không tự kích hoạt lại chính nó.
  function fitHeight(el, table) {
    var lastH = 0;
    function fit() {
      var top = el.getBoundingClientRect().top + window.scrollY;
      var h = Math.max(260, Math.round(window.innerHeight - top - 24));
      if (Math.abs(h - lastH) < 3) { return; } // chênh không đáng kể → bỏ qua, tránh vòng lặp
      lastH = h;
      table.setHeight(h);
      // Layout còn đệm dưới (padding content-wrapper…) → trừ đúng phần dư để trang hết cuộn.
      var doc = document.documentElement;
      var over = doc.scrollHeight - doc.clientHeight;
      if (over > 2) {
        lastH = Math.max(260, h - over);
        table.setHeight(lastH);
      }
    }
    // CHỈ tính lúc dựng xong + khi đổi kích thước cửa sổ.
    // TUYỆT ĐỐI không gắn vào renderComplete: setHeight lại kích hoạt render → vòng lặp treo trang.
    function soon() { requestAnimationFrame(fit); setTimeout(fit, 300); }
    table.on('tableBuilt', soon);
    window.addEventListener('resize', soon);
  }

  // ===== Thanh lọc chuẩn của repo =====
  function wireFilterBar(reload, filterKeys, collectFilters, opts) {
    on('btn-search', reload);
    var q = document.getElementById('f-q');
    if (q) {
      q.addEventListener('keydown', function (e) { if (e.key === 'Enter') { clearTimeout(typing); reload(); } });
      // Gõ tới đâu lọc tới đó (chờ 450ms cho ngưng gõ) — đỡ phải bấm nút Tìm mỗi lần.
      var typing = null;
      q.addEventListener('input', function () {
        clearTimeout(typing);
        typing = setTimeout(reload, 450);
      });
    }

    // Cặp ô …From/…To gộp thành MỘT ô chọn khoảng ngày. Phải chạy TRƯỚC flatpickr bên dưới,
    // nếu không ô gốc đã có altInput riêng và màn hình hiện 2 ô chồng nhau.
    if (tk.dateRanges) { tk.dateRanges(); }
    // Ô ngày lẻ còn lại: gửi ISO cho server, hiện d/m/Y cho người dùng (luật chung của repo).
    if (window.flatpickr) {
      flatpickr('.tk-datef', { dateFormat: 'Y-m-d', altInput: true, altFormat: 'd/m/Y', allowInput: true });
    }
    // Mọi select/input trong panel #adv, hoặc mang class .tk-auto/.tk-s2f, đổi giá trị là tải lại.
    // Bind QUA jQUERY khi có: select2 phát sự kiện change kiểu jQuery, addEventListener thuần
    // KHÔNG nhận được → chọn xong lưới đứng im.
    var sel = '#adv select, #adv input, .tk-auto, .tk-s2f';
    if (window.jQuery) { jQuery(sel).on('change', reload); }
    else { document.querySelectorAll(sel).forEach(function (e) { e.addEventListener('change', reload); }); }

    // Panel lọc nâng cao: ưu tiên tk.advFilter (có badge đếm số lọc đang bật, tự mở khi có lọc).
    if (tk.advFilter && document.getElementById('adv')) {
      tk.advFilter();
    } else {
      var btnAdv = document.getElementById('btn-adv');
      if (btnAdv) {
        btnAdv.addEventListener('click', function () {
          var adv = document.getElementById('adv');
          if (!adv) { return; }
          adv.classList.toggle('d-none');
          var open = !adv.classList.contains('d-none');
          this.classList.toggle('btn-primary', open);
          this.classList.toggle('btn-label-secondary', !open);
        });
      }
    }

    // Chip lọc nhanh: các nhóm trong chipGroups LOẠI TRỪ nhau — bấm lại để bỏ chọn.
    var groups = opts.chipGroups || [];
    function syncChips() {
      document.querySelectorAll('.tk-chip').forEach(function (b) {
        var f = document.getElementById('f-' + b.getAttribute('data-k'));
        var on2 = f && (f.value || '') === b.getAttribute('data-v');
        b.classList.toggle('btn-primary', !!on2);
        b.classList.toggle('btn-label-secondary', !on2);
      });
    }
    document.querySelectorAll('.tk-chip').forEach(function (b) {
      b.addEventListener('click', function () {
        var k = b.getAttribute('data-k'), v = b.getAttribute('data-v');
        var f = document.getElementById('f-' + k);
        if (!f) { return; }
        var next = (f.value || '') === v ? '' : v;
        groups.forEach(function (x) { var e = document.getElementById('f-' + x); if (e) { e.value = ''; } });
        f.value = next;
        syncChips();
        reload();
      });
    });

    on('btn-reset', function () {
      if (q) { q.value = ''; }
      filterKeys.forEach(function (k) {
        var e = document.getElementById('f-' + k);
        if (!e) { return; }
        e.value = '';
        // Select2 giữ giá trị hiển thị riêng — phải báo cho nó vẽ lại, nếu không ô vẫn hiện lựa chọn cũ.
        if (window.jQuery && e.classList.contains('tk-s2f')) { jQuery(e).trigger('change.select2'); }
      });
      document.querySelectorAll('.tk-datef').forEach(function (e) { if (e._flatpickr) { e._flatpickr.clear(); } });
      document.querySelectorAll('#type-tabs .nav-link').forEach(function (a, i) { a.classList.toggle('active', i === 0); });
      syncChips();
      // Màn có giá trị lọc MẶC ĐỊNH (vd hàng chờ duyệt luôn status=0) đặt lại giá trị đó ở đây,
      // trước khi tải — nếu để trang tự set sau thì phải gọi thêm một lượt tải nữa.
      if (opts.onReset) { opts.onReset(); }
      reload();
    });

    // Tab phân loại: gán vào #f-<tabField> (mặc định customerType để tương thích màn khách hàng).
    var tabs = document.getElementById('type-tabs');
    if (tabs) {
      var tabField = tabs.getAttribute('data-field') || 'customerType';
      tabs.addEventListener('click', function (e) {
        var a = e.target.closest('.nav-link'); if (!a) { return; }
        document.querySelectorAll('#type-tabs .nav-link').forEach(function (x) { x.classList.remove('active'); });
        a.classList.add('active');
        var f = document.getElementById('f-' + tabField);
        if (f) { f.value = a.getAttribute('data-type') || ''; }
        reload();
      });
    }

    // Xuất TOÀN BỘ theo bộ lọc đang áp (server-side) — không chỉ trang hiện tại.
    on('btn-export', function () {
      var d = collectFilters();
      var qs = Object.keys(d).map(function (k) {
        return encodeURIComponent(k) + '=' + encodeURIComponent(d[k]);
      }).join('&');
      window.location = (opts.exportUrl || '?handler=Export') + (qs ? '&' + qs : '');
    });
  }

  function on(id, fn) {
    var e = document.getElementById(id);
    if (e) { e.addEventListener('click', fn); }
  }
})();
