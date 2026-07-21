/* tk.js — nền dùng chung mọi màn TourKit (theo docs/UI-CONVENTIONS.md).
   window.tk: post(Result envelope) · toast/error/confirmDelete (SweetAlert2) · money/date format ·
   table (DataTables server-side chuẩn) · form (offcanvas: Select2+flatpickr+jQuery Validate+AJAX). */
(function () {
  'use strict';
  var $ = window.jQuery;
  var tk = {};

  // ---- Antiforgery + POST trả Result { isSuccess, message, data } ----
  tk.token = function () {
    var el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
  };
  tk.post = function (url, formData) {
    return fetch(url, { method: 'POST', headers: { 'RequestVerificationToken': tk.token() }, body: formData })
      .then(function (r) { return r.json(); })
      .catch(function () { return { isSuccess: false, message: 'Lỗi kết nối, thử lại.' }; });
  };

  // ---- Thông báo (SweetAlert2) — KHÔNG dùng alert/confirm trình duyệt ----
  tk.toast = function (msg) {
    if (window.Swal) { Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: msg, showConfirmButton: false, timer: 2200, timerProgressBar: true }); }
  };
  tk.error = function (msg) {
    if (window.Swal) { Swal.fire({ icon: 'error', title: 'Có lỗi', text: msg, confirmButtonText: 'Đóng', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false }); }
    else { alert(msg); }
  };
  tk.confirmDelete = function (opts) {
    opts = opts || {};
    if (!window.Swal) { return Promise.resolve(window.confirm(opts.text || 'Xoá mục này?')); }
    return Swal.fire({
      title: opts.title || 'Xoá?', text: opts.text || 'Thao tác này không thể hoàn tác.', icon: 'warning',
      showCancelButton: true, confirmButtonText: opts.confirm || 'Xoá', cancelButtonText: 'Huỷ',
      customClass: { confirmButton: 'btn btn-danger me-2', cancelButton: 'btn btn-label-secondary' }, buttonsStyling: false
    }).then(function (r) { return r.isConfirmed; });
  };

  // ---- Format VN ----
  tk.money = function (n) { return (Number(n) || 0).toLocaleString('vi-VN'); };
  tk.escape = function (s) { return $('<div>').text(s == null ? '' : s).html(); };
  tk.trunc = function (s, max) { var e = tk.escape(s); return '<span class="d-inline-block text-truncate align-middle" style="max-width:' + (max || 180) + 'px" title="' + e + '">' + e + '</span>'; };

  // ---- DataTables server-side chuẩn (ngôn ngữ VN, fix width, dom Vuexy) ----
  tk.dtLanguage = {
    processing: 'Đang tải...', search: 'Tìm:', lengthMenu: 'Hiện _MENU_ dòng',
    info: 'Hiện _START_–_END_ trên _TOTAL_', infoEmpty: 'Không có dữ liệu', infoFiltered: '(lọc từ _MAX_)',
    zeroRecords: 'Không tìm thấy', emptyTable: 'Chưa có dữ liệu',
    paginate: { first: '«', last: '»', next: '›', previous: '‹' }
  };
  // dom CÓ ô tìm (f) — dùng cho màn KHÔNG có thanh lọc riêng (ô tìm của DataTables là tra cứu duy nhất).
  tk.dtDom = '<"row mx-2 mt-2"<"col-md-6 d-flex align-items-center"l><"col-md-6 d-flex align-items-center justify-content-md-end"f>>t<"row mx-2 my-2"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6 d-flex justify-content-md-end"p>>';
  // dom KHÔNG có ô tìm — dùng khi trang đã có thanh lọc với ô từ khoá (#f-q). Ô "Tìm:" của DataTables
  // lúc đó là ô CHẾT: extraData ghi đè search[value] bằng #f-q nên gõ vào nó không có tác dụng, chỉ gây rối.
  tk.dtDomNoSearch = '<"row mx-2 mt-2"<"col-md-6 d-flex align-items-center"l>>t<"row mx-2 my-2"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6 d-flex justify-content-md-end"p>>';
  // ---- Lọc nâng cao dùng chung ----
  // Trang có panel #adv (các lọc phụ, mặc định ẩn). Trước đây nút mở chỉ là icon trơn nên người
  // dùng không biết market/nhóm/CTV… nằm trong đó. Helper này: gắn/tạo nút "Lọc nâng cao" CÓ NHÃN,
  // badge đếm số lọc đang bật, và tự mở panel khi có lọc (vd vào trang kèm tham số). Đếm dựa TRÊN
  // DOM (ô có giá trị trong #adv) nên không phụ thuộc mảng FILTERS của từng màn.
  tk.advFilter = function () {
    var $panel = $('#adv');
    if (!$panel.length) { return; }

    var $btn = $('#btn-adv');
    if (!$btn.length) {
      var $wrap = $('<div class="mb-1"></div>');
      $btn = $('<button type="button" class="btn btn-sm btn-label-primary" id="btn-adv">' +
        '<i class="ti ti-adjustments-horizontal me-1"></i>Lọc nâng cao' +
        '<span class="badge bg-primary ms-1 d-none" id="adv-count">0</span>' +
        '<i class="ti ti-chevron-down ms-1" id="adv-caret"></i></button>');
      $wrap.append($btn);
      $panel.before($wrap);
    }

    function count() {
      return $panel.find('input, select').filter(function () {
        var v = $(this).val();
        return v != null && String(v).trim() !== '';
      }).length;
    }
    function refresh() {
      var n = count(), $b = $('#adv-count');
      if (n > 0) { $b.text(n).removeClass('d-none'); } else { $b.addClass('d-none'); }
    }
    function toggle(open) {
      var willOpen = open != null ? open : $panel.hasClass('d-none');
      $panel.toggleClass('d-none', !willOpen);
      $('#adv-caret').toggleClass('ti-chevron-up', willOpen).toggleClass('ti-chevron-down', !willOpen);
    }

    $btn.on('click', function () { toggle(); });
    $panel.on('change keyup', 'input, select', refresh);
    // Bấm "Đặt lại" xoá lọc bằng val('') (không kích change) → đếm lại sau một nhịp.
    $('#btn-reset').on('click', function () { setTimeout(refresh, 0); });

    refresh();
    if (count() > 0) { toggle(true); }
  };

  // opts: { url, columns, extraData(d), pageLength }
  tk.table = function (selector, opts) {
    // Trang có thanh lọc riêng (#f-q) → bỏ ô tìm chết của DataTables. Không cần sửa từng màn.
    var hasFilterBar = document.getElementById('f-q') !== null;
    return $(selector).DataTable({
      processing: true, serverSide: true, ordering: false, autoWidth: false,
      ajax: { url: opts.url, data: function (d) { if (opts.extraData) { opts.extraData(d); } } },
      columns: opts.columns,
      lengthMenu: [10, 20, 50, 100], pageLength: opts.pageLength || 20,
      dom: hasFilterBar ? tk.dtDomNoSearch : tk.dtDom, language: tk.dtLanguage
    });
  };

  // Client-side DataTables cho danh mục nhỏ (bảng đã render sẵn ở server): search/paging/entries VN, fix width.
  tk.tableClient = function (selector, opts) {
    opts = opts || {};
    return $(selector).DataTable({
      ordering: opts.ordering || false, autoWidth: false, pageLength: opts.pageLength || 20,
      lengthMenu: [10, 20, 50, 100], dom: tk.dtDom, language: tk.dtLanguage,
      columnDefs: opts.columnDefs || []
    });
  };

  // ---- Offcanvas form engine (Select2 + flatpickr + jQuery Validate + AJAX Result) ----
  // opts: { offcanvas:'#id', form:'#id', table:'#id'|null, saveUrl, title, rules, messages, fill(form,data), afterOpen(data) }
  tk.form = function (opts) {
    var $form = $(opts.form);
    if (!$form.length) { return { open: function () {} }; }
    var ocEl = document.querySelector(opts.offcanvas);

    // Select2 (search + tags multi) trong offcanvas
    if ($.fn.select2) {
      $form.find('.tk-s2').each(function () { $(this).select2({ dropdownParent: $(ocEl), width: '100%', allowClear: true, placeholder: $(this).data('placeholder') || '' }); });
      $form.find('.tk-s2-tags').each(function () { $(this).select2({ dropdownParent: $(ocEl), width: '100%', multiple: true, placeholder: $(this).data('placeholder') || '' }); });
    }
    // flatpickr
    if (window.flatpickr) { $form.find('.tk-date').each(function () { if (!this._flatpickr) { flatpickr(this, { altInput: true, altFormat: 'd/m/Y', dateFormat: 'Y-m-d', allowInput: true }); } }); }
    // jQuery Validate
    if ($.fn.validate) {
      $form.validate({
        ignore: ':hidden', errorElement: 'span', errorClass: 'text-danger d-block small mt-1',
        rules: opts.rules || {}, messages: opts.messages || {},
        highlight: function (el) { $(el).addClass('is-invalid'); }, unhighlight: function (el) { $(el).removeClass('is-invalid'); }
      });
    }

    function setField(name, v) {
      var el = $form.find('[name="' + name + '"]')[0];
      if (!el) { return; }
      if (el._flatpickr) { v ? el._flatpickr.setDate(v, true) : el._flatpickr.clear(); return; }
      if ($(el).hasClass('tk-s2') || $(el).hasClass('tk-s2-tags')) { $(el).val(v == null || v === '' ? null : v).trigger('change'); return; }
      if (el.type === 'checkbox') { el.checked = !!v; return; }
      el.value = (v == null) ? '' : v;
    }

    function open(data) {
      if ($.fn.validate) { $form.validate().resetForm(); }
      // Điền: mọi [name] có dạng "Input.X"/"Id" → data[camelCase]
      $form.find('[name]').each(function () {
        var name = this.getAttribute('name');
        var key = name.replace(/^Input\./, '');
        key = key.charAt(0).toLowerCase() + key.slice(1);
        setField(name, data ? data[key] : (this.type === 'checkbox' ? false : ''));
      });
      if (opts.fill) { opts.fill($form, data); }
      var lbl = ocEl.querySelector('.offcanvas-title');
      if (lbl && opts.title) { lbl.textContent = (data ? ('Sửa ' + opts.title) : ('Thêm ' + opts.title)); }
      if (opts.afterOpen) { opts.afterOpen(data); }
      bootstrap.Offcanvas.getOrCreateInstance(ocEl).show();
    }

    $form.on('submit', function (e) {
      e.preventDefault();
      if ($.fn.validate && !$form.valid()) { return; }
      tk.post($form.attr('data-action') || opts.saveUrl, new FormData(this)).then(function (res) {
        if (res && res.isSuccess) {
          bootstrap.Offcanvas.getInstance(ocEl).hide();
          var dt = opts.table && $.fn.DataTable.isDataTable(opts.table) ? $(opts.table).DataTable() : null;
          var serverSide = dt && dt.settings()[0].oInit.serverSide;
          if (serverSide) { dt.ajax.reload(null, false); tk.toast(res.message || 'Đã lưu.'); }
          else { location.reload(); }   // client-side: re-render dòng từ server
        } else { tk.error((res && (res.message || res.detail || res.title)) || 'Lưu thất bại.'); }
      });
    });

    return { open: open };
  };


  // ---- Panel thông tin trượt từ PHẢI (offcanvas) ----
  // Dùng cho các chỗ chỉ cần liếc nhanh rồi đi tiếp: bấm sự kiện trên lịch, bấm ô lưới…
  // Điều hướng hẳn sang trang khác làm mất ngữ cảnh (đang xem tháng nào, cuộn tới đâu);
  // panel giữ nguyên màn hình phía sau, đóng lại là xem tiếp được ngay.
  // Panel được tạo MỘT lần rồi dùng lại, không nhồi thêm DOM mỗi lần bấm.
  tk.panel = function (opts) {
    opts = opts || {};
    var el = document.getElementById('tk-panel');
    if (!el) {
      el = document.createElement('div');
      el.id = 'tk-panel';
      el.className = 'offcanvas offcanvas-end';
      el.tabIndex = -1;
      el.style.width = '420px';
      el.style.maxWidth = '100%';
      el.innerHTML =
        '<div class="offcanvas-header border-bottom">' +
          '<h5 class="offcanvas-title" id="tk-panel-title"></h5>' +
          '<button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Đóng"></button>' +
        '</div>' +
        '<div class="offcanvas-body"><div id="tk-panel-body"></div></div>';
      document.body.appendChild(el);
    }

    document.getElementById('tk-panel-title').textContent = opts.title || '';

    var html = '';
    (opts.rows || []).forEach(function (r) {
      if (!r || r.value == null || r.value === '') { return; }
      html += '<div class="d-flex justify-content-between align-items-start gap-3 py-2 border-bottom">' +
                '<span class="text-muted flex-shrink-0">' + tk.escape(r.label) + '</span>' +
                '<span class="fw-medium text-end">' + (r.html ? r.value : tk.escape(String(r.value))) + '</span>' +
              '</div>';
    });
    if (opts.actionUrl) {
      html += '<a href="' + opts.actionUrl + '" class="btn btn-primary w-100 mt-3">' +
              tk.escape(opts.actionText || 'Xem chi tiết') + '</a>';
    }
    document.getElementById('tk-panel-body').innerHTML = html;

    bootstrap.Offcanvas.getOrCreateInstance(el).show();
  };

  window.tk = tk;
})();
