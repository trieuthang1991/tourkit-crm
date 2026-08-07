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
  // ---- Gộp cặp ngày "từ – đến" thành MỘT ô date-range ----
  // Trước đây mỗi khoảng ngày chiếm 2 cột (Ngày tạo từ | đến) trông rời rạc, kém chuyên nghiệp.
  // Helper tự tìm cặp #f-{k}From + #f-{k}To (đều là .tk-datef), gộp thành một ô flatpickr range,
  // rồi GHI NGƯỢC giá trị (ISO) về 2 ô gốc (ẩn đi) — nhờ vậy toàn bộ code lọc cũ đọc f-…From/To
  // không phải sửa gì. Chạy TRƯỚC lúc trang khởi tạo flatpickr('.tk-datef') nên không đụng nhau.
  function isoDate(d) {
    var m = d.getMonth() + 1, day = d.getDate();
    return d.getFullYear() + '-' + (m < 10 ? '0' + m : m) + '-' + (day < 10 ? '0' + day : day);
  }
  tk.dateRanges = function () {
    if (!window.flatpickr) { return; }
    $('input.tk-datef[id$="From"]').each(function () {
      var fromInput = this;
      var toInput = document.getElementById(fromInput.id.replace(/From$/, 'To'));
      if (!toInput || !$(toInput).hasClass('tk-datef')) { return; }

      // Trang nào lỡ flatpickr 2 ô gốc TRƯỚC (script chạy ngay, không đợi ready) thì ô gốc đã có
      // altInput hiển thị riêng — chỉ addClass('d-none') vào ô gốc không giấu được altInput, màn hình
      // hiện ra 2 ô chồng nhau ("Ngày tạo (từ – đến)" + "Ngày tạo từ"). Huỷ flatpickr để gỡ altInput.
      if (fromInput._flatpickr) { fromInput._flatpickr.destroy(); }
      if (toInput._flatpickr) { toInput._flatpickr.destroy(); }

      var $fromCol = $(fromInput).closest('[class*="col-"]');
      var $toCol = $(toInput).closest('[class*="col-"]');
      // Nhãn: bỏ chữ "từ" ở cuối ("Ngày tạo từ" → "Ngày tạo").
      var $lbl = $fromCol.find('label').first();
      if ($lbl.length) { $lbl.text($lbl.text().replace(/\s*từ\s*$/i, '').trim()); }

      // Vô hiệu 2 ô gốc (gỡ tk-datef để trang không flatpickr chúng) và ẩn đi (giữ để chứa giá trị).
      $(fromInput).add(toInput).removeClass('tk-datef').addClass('d-none');
      $toCol.addClass('d-none');

      // Placeholder gợi nghĩa (đồng nhất KHÔNG label): nếu ô "From" đặt placeholder kết thúc bằng "từ"
      // (vd "Ngày tạo từ") thì range hiện "Ngày tạo (từ – đến)". Không có → dùng mặc định dd/mm/yyyy.
      var rawPh = ($(fromInput).attr('placeholder') || '').trim();
      var mBase = rawPh.match(/^(.*?)\s*(từ|from)$/i);
      var rangePh = mBase ? (mBase[1].trim() + ' (từ – đến)') : 'dd/mm/yyyy – dd/mm/yyyy';
      var $range = $('<input type="text" class="form-control" autocomplete="off">').attr('placeholder', rangePh);
      $(fromInput).after($range);

      var fp = flatpickr($range[0], {
        mode: 'range', dateFormat: 'd/m/Y', locale: { rangeSeparator: ' – ' }, allowInput: false,
        onChange: function (sel) {
          $(fromInput).val(sel[0] ? isoDate(sel[0]) : '').trigger('change');
          $(toInput).val(sel[1] ? isoDate(sel[1]) : '').trigger('change');
        }
      });
      // Nạp sẵn nếu vào trang đã có giá trị (vd lọc kèm tham số).
      var seed = [fromInput.value, toInput.value].filter(Boolean);
      if (seed.length) { fp.setDate(seed, false, 'Y-m-d'); }
      // Nút "Đặt lại" xoá lọc → xoá luôn ô range.
      $('#btn-reset').on('click', function () { setTimeout(function () { fp.clear(); }, 0); });
    });
  };

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
      // Nút icon nhỏ, gọn — chỉ icon phễu + badge đếm nổi ở góc khi có lọc đang bật (giữ tín hiệu
      // "đang lọc" mà không chiếm chỗ), tooltip "Lọc nâng cao". Đặt cạnh nút Tìm/Đặt lại nếu có
      // nhóm nút (#btn-search), không thì đứng riêng trên panel.
      $btn = $('<button type="button" class="btn btn-icon btn-sm btn-label-secondary position-relative" id="btn-adv" title="Lọc nâng cao" data-bs-toggle="tooltip">' +
        '<i class="ti ti-filter"></i>' +
        '<span class="badge rounded-pill bg-primary position-absolute top-0 start-100 translate-middle d-none" id="adv-count" style="font-size:.6rem;padding:.2em .4em">0</span>' +
        '</button>');
      var $group = $('#btn-search').parent();
      if ($group.length && $group.hasClass('d-flex')) { $group.append($btn); }
      else { $panel.before($('<div class="mb-1"></div>').append($btn)); }
    }

    function count() {
      // Bỏ ô .d-none (2 ô gốc từ/đến đã bị gộp date-range ẩn đi) để một khoảng ngày chỉ tính LÀ 1,
      // không tính thành 3 (ô range + 2 ô ẩn). Select2 ẩn native bằng class khác nên vẫn được đếm.
      return $panel.find('input, select').not('.d-none').filter(function () {
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

  // ---- Ô soạn thảo có ĐỊNH DẠNG cho trường lưu HTML ----
  // Dùng Quill — bộ soạn thảo ĐÃ đóng gói sẵn trong theme (vendor/libs/quill). Dự án KHÔNG có
  // TinyMCE; thêm nó là thêm một thư viện ngoài nữa cho cùng một việc.
  // Textarea gốc vẫn là nơi GIỮ giá trị (chỉ ẩn đi) nên FormData, jQuery Validate và tk.form
  // không phải sửa gì — chỉ thêm class `tk-rte` vào textarea là xong.
  tk.rte = function (el) {
    el = typeof el === 'string' ? document.querySelector(el) : el;
    if (!el || el._quill || !window.Quill) { return null; }

    var wrap = document.createElement('div');
    wrap.className = 'tk-rte';
    var host = document.createElement('div');
    host.style.minHeight = (el.rows > 4 ? 220 : 160) + 'px';
    wrap.appendChild(host);
    el.parentNode.insertBefore(wrap, el);
    el.classList.add('d-none');

    var q = new Quill(host, {
      theme: 'snow',
      placeholder: el.getAttribute('placeholder') || '',
      modules: {
        // Chỉ những thứ soạn nội dung thật cần. Bảng màu, cỡ chữ, font… để người dùng tự do
        // là mở đường cho email/bài viết mỗi cái một kiểu.
        toolbar: [
          [{ header: [2, 3, false] }],
          ['bold', 'italic', 'underline'],
          [{ list: 'ordered' }, { list: 'bullet' }],
          ['link', 'blockquote'],
          ['clean']
        ]
      }
    });
    // Rỗng thì trả CHUỖI RỖNG, đừng trả '<p><br></p>' — server sẽ tưởng là có nội dung.
    q.on('text-change', function () {
      el.value = q.getText().trim() === '' ? '' : q.root.innerHTML;
    });
    el._quill = q;
    return q;
  };

  // Đổi qua lại giữa soạn thảo có định dạng và ô văn bản thường (vd: kênh Email dùng HTML,
  // SMS/Zalo là văn bản thuần — cho gõ HTML vào SMS là gửi đi một mớ thẻ).
  tk.rte.enable = function (el, on) {
    el = typeof el === 'string' ? document.querySelector(el) : el;
    if (!el) { return; }
    if (on && !el._quill) { tk.rte(el); }
    var wrap = el._quill ? el._quill.container.parentNode : null;
    if (!wrap) { return; }
    if (on) { tk.rte.set(el, el.value); }
    wrap.classList.toggle('d-none', !on);
    el.classList.toggle('d-none', !!on);
  };

  // Đổ nội dung vào ô soạn thảo (dùng khi mở form sửa).
  tk.rte.set = function (el, html) {
    if (!el || !el._quill) { return; }
    el._quill.clipboard.dangerouslyPasteHTML(html || '');
    el.value = html || '';
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
    // Trường lưu HTML: dựng ô soạn thảo có định dạng ngay khi lập form.
    $form.find('textarea.tk-rte-field').each(function () { tk.rte(this); });
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
      // Thêm mới: giá trị rỗng gán vào <select> KHÔNG có mục rỗng làm ô trắng trơn, người dùng
      // tưởng chưa chọn được gì. Rơi về mục đầu tiên — cũng chính là mặc định của server.
      if (el.tagName === 'SELECT' && el.selectedIndex === -1) { el.selectedIndex = 0; }
      // Ô soạn thảo giữ nội dung trong Quill, gán value cho textarea ẩn là chưa đủ.
      if (el._quill) { tk.rte.set(el, el.value); }
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
          // Màn dùng tk.grid (Tabulator) không có DataTable — nạp lại lưới tại chỗ, đừng tải lại cả trang.
          else if (window.tkGridReload) { window.tkGridReload(); tk.toast(res.message || 'Đã lưu.'); }
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

  // Gộp date-range TỰ ĐỘNG trên mọi trang. Đăng ký ở đây (tk.js nạp trước) nên callback này chạy
  // TRƯỚC $(function) của từng trang → vô hiệu ô gốc xong mới tới lúc trang flatpickr('.tk-datef').
  $(function () { tk.dateRanges(); });
})();
