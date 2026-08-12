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
  // Ô ĐỔI ĐƯỢC ngay trên bảng (trạng thái / loại / phân loại…): badge hiện tại + mũi tên,
  // dựng bằng <button> nên có nền khi rê chuột và bắt được bàn phím — người dùng nhìn là biết bấm được.
  // Dùng kèm `clickMenu` của cột: { field:'status', clickMenu: fn, formatter: () => g.pick(label, color) }.
  g.pick = function (label, color, title) {
    return '<button type="button" class="tk-pick" title="' + esc(title || 'Bấm để đổi') + '">' +
      '<span class="badge bg-label-' + esc(color || 'secondary') + '">' + esc(label || '—') + '</span>' +
      '<i class="ti ti-chevron-down"></i></button>';
  };
  // Ô tiền: số đậm bên phải + ghi chú mờ bên dưới.
  g.moneyCell = function (value, sub) {
    return '<div class="tk-cell tk-cell-right"><div class="tk-cell-main ' + ((value || 0) > 0 ? 'text-success' : '') + '">' +
      money(value) + '</div>' + (sub ? '<div class="tk-cell-sub">' + esc(sub) + '</div>' : '') + '</div>';
  };
  tk.g = g;

  // ===== Formatter dùng chung của Tabulator =====
  // Mọi ô "trạng thái / loại / phân loại" phải vẽ badge QUA ĐÂY, không mỗi màn tự nối chuỗi HTML.
  //   { field:'statusLabel', formatter:'tkBadge', formatterParams:{ color:'statusColor', sub:'note' } }
  //   color: tên trường chứa màu (primary/success/…) hoặc chuỗi màu cố định.
  if (window.Tabulator && Tabulator.extendModule) {
    Tabulator.extendModule('format', 'formatters', {
      tkBadge: function (cell, params) {
        var label = cell.getValue();
        if (label == null || label === '') { return '<span class="tk-cell-sub">—</span>'; }
        var row = cell.getData();
        var p = params || {};
        var color = (p.color && row[p.color]) || p.color || 'secondary';
        var sub = p.sub ? row[p.sub] : null;
        var badge = '<span class="badge bg-label-' + esc(color) + '">' + esc(label) + '</span>';
        if (!sub) { return badge; }
        return '<div class="tk-cell">' + badge + '<div class="tk-cell-sub mt-1" title="' + esc(sub) + '">' + esc(sub) + '</div></div>';
      },
      // Ô 2 dòng chuẩn: giá trị của cột là dòng chính, `sub` trỏ tới trường làm dòng phụ.
      tkStack: function (cell, params) {
        var p = params || {};
        var row = cell.getData();
        return g.stack(cell.getValue(), p.sub ? row[p.sub] : null);
      },
      // Ô có avatar chữ cái + 2 dòng (dùng cho cột tên khách/NCC/nhân sự).
      tkMedia: function (cell, params) {
        var p = params || {};
        var row = cell.getData();
        var v = cell.getValue();
        if (!v) { return '<span class="tk-cell-sub">—</span>'; }
        return '<div class="tk-row-media">' + g.avatar(v, !!p.small) + g.stack(v, p.sub ? row[p.sub] : null) + '</div>';
      }
    });
  }

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

    /**
     * Dòng TỔNG không phải một bản ghi, nên không được mang menu hành động.
     *
     * Tabulator dựng dòng tổng bằng cùng loại phần tử với dòng dữ liệu, nên nếu không loại nó ra thì
     * bấm vào ô hành động của dòng tổng vẫn mở ra "Sửa …" — với dữ liệu là các con số CỘNG DỒN và
     * không có id. Bấm Lưu ở đó tạo ra một bản ghi rác mang giá trị bằng tổng cả trang, còn bấm Xoá
     * thì gọi xoá với id rỗng.
     */
    function laDongTong(row) {
      try { return row.getElement().classList.contains('tabulator-calcs'); } catch (e) { return true; }
    }

    function rowActionMenu(e, row) { return laDongTong(row) ? [] : actionsFor(row.getData()); }
    function cellActionMenu(e, cell) {
      var row = cell.getRow();
      return laDongTong(row) ? [] : actionsFor(row.getData());
    }

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
        // Tiêu đề cột này mang nút "chỉnh bảng" cố định (ẩn/hiện cột) — một chỗ duy nhất,
        // luôn nhìn thấy, thay vì rải icon trên từng cột.
        title: '', field: '__act', width: 56, hozAlign: 'center', headerSort: false,
        headerMenu: columnMenu, headerMenuIcon: '<i class="ti ti-adjustments-horizontal" title="Ẩn/hiện cột"></i>',
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
            // Đầu/Trước/Sau/Cuối dùng ICON cho gọn; chữ vẫn giữ ở *_title làm tooltip
            // (Tabulator dựng nút bằng innerHTML nên nhận được markup icon).
            page_size: 'Số dòng', page_title: 'Tới trang',
            first: '<i class="ti ti-chevrons-left"></i>', first_title: 'Trang đầu',
            last: '<i class="ti ti-chevrons-right"></i>', last_title: 'Trang cuối',
            prev: '<i class="ti ti-chevron-left"></i>', prev_title: 'Trang trước',
            next: '<i class="ti ti-chevron-right"></i>', next_title: 'Trang sau',
            all: 'Tất cả',
            counter: { showing: 'Hiện', of: 'trên', rows: 'dòng', pages: 'trang' }
          },
          // Cùng một chỉ báo với DataTables (xem tk.dtLanguage.processing): hai lưới nằm cạnh nhau
          // trong cùng sản phẩm mà mỗi cái quay một kiểu thì trông như hai phần mềm ghép lại.
          data: {
            loading: '<span class="tk-load"><span class="tk-load-spin"></span>Đang tải…</span>',
            error: '<span class="tk-load tk-load-loi"><i class="ti ti-alert-triangle me-2"></i>Lỗi tải dữ liệu</span>'
          }
        }
      },
      layout: 'fitColumns',
      // Chiều cao tính theo màn hình (fitHeight bên dưới) → bảng LUÔN gọn trong 1 màn,
      // cuộn diễn ra NGAY TRONG bảng nên chỉ có MỘT thanh cuộn, không cuộn trang.
      height: 420,
      // Bảng rỗng phải NÓI ĐƯỢC phải làm gì tiếp, đừng để mỗi dòng chữ "Không có dữ liệu".
      placeholder: '<div class="tk-empty"><i class="ti ti-inbox"></i>' +
        '<div class="tk-empty-title">' + esc(opts.emptyTitle || 'Chưa có dữ liệu') + '</div>' +
        '<div class="tk-empty-hint">' + esc(opts.emptyHint || 'Thử nới bộ lọc hoặc bấm Đặt lại để xem toàn bộ danh sách.') + '</div></div>',
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
      columnDefaults: {
        headerSort: false, resizable: true, vertAlign: 'middle',
        // Nút ẩn/hiện cột nằm ở MỌI tiêu đề (đúng lối của Tabulator): ở đâu thấy vướng cột
        // thì tắt ngay tại đó, không phải đi tìm một nút cấu hình nào khác.
        headerMenu: columnMenu,
        headerMenuIcon: '<i class="ti ti-adjustments-horizontal"></i>'
      },
      columns: columns
    };
    if (opts.actions) { config.rowContextMenu = rowActionMenu; }
    if (opts.onRowClick) { el.classList.add('tk-grid-clickable'); }
    if (selectable) {
      // 'highlight' = CHỈ ô chọn mới tích/bỏ tích. Để `true` thì bấm vào ô dữ liệu bất kỳ cũng
      // tích dòng, người dùng định mở chi tiết lại thành chọn dòng.
      // KHÔNG dùng selectableRowsRangeMode:'click' — chế độ đó coi mỗi click là chọn một VÙNG mới
      // nên bấm ô thứ hai lại bỏ ô thứ nhất (chỉ chọn được 1).
      config.selectableRows = 'highlight';
    }
    Object.keys(opts.tabular || {}).forEach(function (k) { config[k] = opts.tabular[k]; });

    var table = new Tabulator(selector, config);
    // Khoá lưu lựa chọn cột gắn theo selector → một trang có 2 lưới thì mỗi lưới nhớ riêng.
    el.__tkSelector = selector;
    table.on('tableBuilt', function () { restoreCols(table, selector); });

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
    tameMenus();
    fitHeight(el, table);
    if (opts.wireFilters !== false) { wireFilterBar(reload, filterKeys, collectFilters, opts); }

    return { table: table, reload: reload, filters: collectFilters };
  };

  // ===== Thanh tác vụ hàng loạt (#bulkbar) — chỉ hoạt động nếu trang có sẵn khối này =====
  function wireBulkBar(table, reload, opts) {
    var bulkbar = document.getElementById('bulkbar');
    if (!bulkbar) { return; }
    // Thanh tác vụ NỔI ngay trên bảng: hiện ra không đẩy nội dung, không đẻ thêm khối mới
    // (trước đây nó là một dải riêng nên mỗi lần chọn dòng là bảng bị đội cao thêm).
    bulkbar.classList.add('tk-bulkbar');
    bulkbar.style.background = '';
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

  // ===== Ẩn / hiện cột =====
  // Bảng nghiệp vụ nào cũng nhiều cột hơn số cột một người thực sự theo dõi hằng ngày.
  // Cho tắt bớt cột ngay tại tiêu đề, và NHỚ lựa chọn đó theo từng màn (localStorage) —
  // tắt xong tải lại trang mà cột hiện lại hết thì coi như không có tính năng này.
  var COLS_KEY = 'tk.cols:';

  function colsKey(selector) { return COLS_KEY + location.pathname + selector; }

  function readCols(selector) {
    var raw;
    try { raw = localStorage.getItem(colsKey(selector)); } catch (e) { return {}; }
    if (!raw) { return {}; }
    try {
      var v = JSON.parse(raw);
      return (v && typeof v === 'object' && !Array.isArray(v)) ? v : {};
    } catch (e) { return {}; }
  }

  // Lưu theo kiểu { field: hiện/ẩn } và CHỈ ghi cột người dùng tự bật/tắt.
  // Nếu lưu "mọi cột đang ẩn" thì các cột do TRANG tự ẩn theo loại đơn (vd cột Visa chỉ hiện ở
  // đơn Visa) cũng bị ghi vào, sang màn Visa lại mất cột dù người dùng chưa hề tắt.
  function saveCol(selector, field, visible) {
    if (!field) { return; }
    var map = readCols(selector);
    map[field] = !!visible;
    try { localStorage.setItem(colsKey(selector), JSON.stringify(map)); } catch (e) { /* chế độ riêng tư */ }
  }

  function restoreCols(table, selector) {
    var map = readCols(selector);
    Object.keys(map).forEach(function (f) {
      // Cột kỹ thuật (ô chọn, ⋮) không nằm trong danh sách chọn nên cũng không được đụng vào.
      if (f === '__sel' || f === '__act') { return; }
      var col = table.getColumn(f);
      if (!col) { return; }
      if (map[f]) { col.show(); } else { col.hide(); }
    });
  }

  // Danh sách cột để tích/bỏ tích. Bấm một mục KHÔNG đóng menu (stopPropagation) để bật/tắt
  // liền mấy cột trong một lần mở.
  function columnMenu(e, column) {
    var table = column.getTable();
    var selector = table.element.__tkSelector || '';
    var data = table.getColumns().filter(function (c) {
      var f = c.getField();
      return f && f !== '__sel' && f !== '__act' && c.getDefinition().title;
    });

    var items = [{
      label: '<span class="text-muted"><i class="ti ti-eye me-2"></i>Hiện tất cả cột</span>',
      action: function (ev) {
        ev.stopPropagation();
        data.forEach(function (c) { c.show(); saveCol(selector, c.getField(), true); });
        ev.currentTarget.parentNode.querySelectorAll('input[type="checkbox"]').forEach(function (b) { b.checked = true; });
      }
    }, { separator: true }];

    data.forEach(function (col) {
      items.push({
        label: '<label class="tk-colpick"><input type="checkbox"' + (col.isVisible() ? ' checked' : '') + '>' +
          '<span>' + esc(col.getDefinition().title) + '</span></label>',
        action: function (ev) {
          ev.stopPropagation();
          var visible = data.filter(function (c) { return c.isVisible(); });
          // Ẩn hết cột thì còn lại một bảng trống — chặn ngay ở nước cuối cùng.
          if (col.isVisible() && visible.length <= 1) {
            var box0 = ev.currentTarget.querySelector('input');
            if (box0) { box0.checked = true; }
            return;
          }
          col.toggle();
          var box = ev.currentTarget.querySelector('input');
          if (box) { box.checked = col.isVisible(); }
          saveCol(selector, col.getField(), col.isVisible());
        }
      });
    });
    return items;
  }

  // ===== Nắn menu của Tabulator cho vừa màn hình =====
  // Tabulator dựng menu ngay tại toạ độ chuột. Khi bên dưới không còn chỗ, Popup._fitToScreen
  // KHÔNG lật menu lên mà ÉP `height` = chiều cao cả trang → hộp trắng khổng lồ (đúng cái
  // "lỗi dropdown" hay gặp khi bấm ⋮ / đổi trạng thái ở những dòng cuối bảng), đồng thời kéo
  // dài tài liệu nên trang mọc thêm THANH CUỘN NGOÀI.
  // Ở đây: bỏ chiều cao bị ép, chuyển sang position:fixed (không tính vào chiều cao trang nữa)
  // rồi tự lật lên/kẹp lại trong viewport.
  function tameMenus() {
    if (window.__tkMenuTamed) { return; }
    window.__tkMenuTamed = true;
    // MutationObserver chạy sau khi Tabulator append + _fitToScreen xong (microtask, chưa vẽ lại)
    // nên ta luôn là người đặt vị trí cuối cùng, và không thấy nhấp nháy.
    new MutationObserver(function (recs) {
      recs.forEach(function (rec) {
        Array.prototype.forEach.call(rec.addedNodes, function (n) {
          if (n.nodeType === 1 && n.classList && n.classList.contains('tabulator-menu')) { placeMenu(n); }
        });
      });
    }).observe(document.body, { childList: true, subtree: true });
  }

  function placeMenu(m) {
    m.style.height = '';                        // gỡ chiều cao bị ép
    var r = m.getBoundingClientRect();          // vị trí Tabulator vừa đặt, quy về toạ độ màn hình
    var pad = 8;
    var h = m.offsetHeight, w = m.offsetWidth;
    var top = r.top;
    if (top + h > window.innerHeight - pad) {
      var above = r.top - h;                    // ưu tiên LẬT LÊN TRÊN điểm bấm
      top = above >= pad ? above : Math.max(pad, window.innerHeight - h - pad);
    }
    var left = Math.max(pad, Math.min(r.left, window.innerWidth - w - pad));
    m.style.position = 'fixed';
    m.style.top = Math.round(top) + 'px';
    m.style.left = Math.round(left) + 'px';
    m.style.right = 'auto';
    m.style.bottom = 'auto';
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
      // Lặp vài lượt: mỗi lần đổi chiều cao bảng, footer/thanh cuộn trong bảng lại tính lại nên
      // một lượt trừ thường còn sót vài pixel — mà chỉ vài pixel dư là trang đã có thanh cuộn.
      var doc = document.documentElement;
      for (var i = 0; i < 4; i++) {
        var over = doc.scrollHeight - doc.clientHeight;
        if (over <= 2) { break; }
        lastH = Math.max(260, lastH - over);
        table.setHeight(lastH);
      }
    }
    // CHỈ tính lúc dựng xong + khi đổi kích thước cửa sổ.
    // TUYỆT ĐỐI không gắn vào renderComplete: setHeight lại kích hoạt render → vòng lặp treo trang.
    function soon() { requestAnimationFrame(fit); setTimeout(fit, 300); }
    table.on('tableBuilt', soon);
    window.addEventListener('resize', soon);
    // Mở panel "Lọc nâng cao", dải thống kê xuống dòng, chip lọc thêm hàng… đều đẩy đỉnh bảng
    // xuống. Không tính lại thì trang dài thêm đúng bằng phần đó và mọc THANH CUỘN NGOÀI.
    // Chỉ theo dõi các khối ANH EM của bảng — chiều cao của chúng không phụ thuộc setHeight
    // nên không tạo vòng lặp quan sát.
    if (window.ResizeObserver && el.parentElement) {
      var ro = new ResizeObserver(soon);
      Array.prototype.forEach.call(el.parentElement.children, function (c) { if (c !== el) { ro.observe(c); } });
    }
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
