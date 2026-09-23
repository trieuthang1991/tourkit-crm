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

  // ---- Lớp phủ ĐANG XỬ LÝ (linh vật robot bay) — cho lúc lưu form / thao tác chờ máy chủ ----
  // Dùng lại thẻ .tk-load (mascot) trên nền mờ toàn màn. Đếm tham chiếu để nhiều lời gọi lồng nhau
  // không tắt sớm. show(text) đổi được dòng chữ; mỗi show phải có đúng một hide (nên gọi ở .finally).
  tk.overlay = (function () {
    var el = null, dem = 0;
    function ensure() {
      if (el) { return el; }
      el = document.createElement('div');
      el.className = 'tk-overlay';
      el.innerHTML = '<span class="tk-load"><span class="tk-load-spin"></span><span class="tk-load-txt">Đang xử lý…</span></span>';
      document.body.appendChild(el);
      return el;
    }
    return {
      show: function (text) {
        var o = ensure();
        var t = o.querySelector('.tk-load-txt');
        if (t) { t.textContent = text || 'Đang xử lý…'; }
        dem++; o.classList.add('is-on');
      },
      hide: function () {
        dem = Math.max(0, dem - 1);
        if (dem === 0 && el) { el.classList.remove('is-on'); }
      }
    };
  })();

  // ---- Popup XUẤT FILE dùng chung -----------------------------------------------------------------
  // Bấm "Xuất file" mở popup chọn SỐ DÒNG xuất theo bộ lọc đang áp — để xuất được NHIỀU HƠN số đang
  // hiển thị trên trang. Modal dựng MỘT lần rồi dùng lại (như tk.panel).
  // opts: { url:'?handler=Export', filters:{...}, filteredTotal:Number|null, pageSize:Number, max:Number }
  tk.exportDialog = function (opts) {
    if (!window.bootstrap) { return; }
    opts = opts || {};
    var max = opts.max || 5000;
    var pageSize = opts.pageSize || 20;
    var total = (opts.filteredTotal != null && opts.filteredTotal >= 0) ? Number(opts.filteredTotal) : null;
    function vn(n) { return Number(n).toLocaleString('vi-VN'); }

    var el = document.getElementById('tk-export-modal');
    if (!el) {
      el = document.createElement('div');
      el.id = 'tk-export-modal';
      el.className = 'modal fade';
      el.tabIndex = -1;
      el.innerHTML =
        '<div class="modal-dialog modal-dialog-centered">' +
          '<div class="modal-content">' +
            '<div class="modal-header"><h5 class="modal-title"><i class="ti ti-file-export me-2"></i>Xuất file</h5>' +
              '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button></div>' +
            '<div class="modal-body">' +
              '<p class="mb-3" id="tk-export-note"></p>' +
              '<label class="form-label" for="tk-export-count">Số dòng xuất</label>' +
              '<select class="form-select" id="tk-export-count"></select>' +
              '<div class="form-text">Xuất theo bộ lọc đang áp — có thể nhiều hơn số hiển thị trên trang. Tối đa <span id="tk-export-max"></span> dòng.</div>' +
            '</div>' +
            '<div class="modal-footer">' +
              '<button type="button" class="btn btn-label-secondary" data-bs-dismiss="modal">Huỷ</button>' +
              '<button type="button" class="btn btn-primary" id="tk-export-go"><i class="ti ti-download me-1"></i>Xuất</button>' +
            '</div>' +
          '</div>' +
        '</div>';
      document.body.appendChild(el);
    }

    el.querySelector('#tk-export-note').innerHTML = total != null
      ? 'Bộ lọc hiện tại khớp <b>' + vn(total) + '</b> dòng.'
      : 'Xuất toàn bộ theo bộ lọc đang áp.';
    el.querySelector('#tk-export-max').textContent = vn(max);

    var allN = Math.min(total != null ? total : max, max);
    // "Tất cả theo lọc" luôn là lựa chọn CHÍNH (mặc định); thêm các mốc NHỎ HƠN tổng để xuất bớt nếu muốn.
    var opList = [{ v: allN, t: total != null ? ('Tất cả theo lọc (' + vn(allN) + ' dòng)') : ('Tất cả (tối đa ' + vn(max) + ' dòng)') }];
    [pageSize, 500, 1000, 2000, 5000].forEach(function (n) {
      if (n > 0 && n < allN) { opList.push({ v: n, t: n === pageSize ? ('Trang hiện tại (' + n + ' dòng)') : (vn(n) + ' dòng') }); }
    });
    var sel = el.querySelector('#tk-export-count');
    sel.innerHTML = opList.map(function (o) { return '<option value="' + o.v + '">' + o.t + '</option>'; }).join('');
    sel.value = String(allN);   // mặc định: tất cả theo lọc

    el.querySelector('#tk-export-go').onclick = function () {
      var f = opts.filters || {};
      var qs = Object.keys(f).map(function (k) { return encodeURIComponent(k) + '=' + encodeURIComponent(f[k]); }).join('&');
      var url = (opts.url || '?handler=Export') + (qs ? '&' + qs : '') + '&limit=' + encodeURIComponent(sel.value);
      try { bootstrap.Modal.getInstance(el).hide(); } catch (e) {}
      window.location = url;
    };

    bootstrap.Modal.getOrCreateInstance(el).show();
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

  /**
   * Hỏi lại trước một thao tác ĐƯỢC PHÉP nhưng có hệ quả người dùng nên biết.
   *
   * Khác confirmDelete: nút chính màu chủ đạo chứ không phải màu đỏ, và chữ mặc định không nói
   * "không thể hoàn tác". Dùng màu đỏ cho việc hợp lệ khiến người dùng quen tay bấm bừa, tới lúc
   * gặp hộp xoá thật thì cũng bấm bừa nốt.
   */
  tk.confirmWarn = function (opts) {
    opts = opts || {};
    if (!window.Swal) { return Promise.resolve(window.confirm(opts.text || 'Tiếp tục?')); }
    return Swal.fire({
      title: opts.title || 'Xác nhận', text: opts.text || '', icon: 'warning',
      showCancelButton: true, confirmButtonText: opts.confirm || 'Tiếp tục', cancelButtonText: 'Huỷ',
      customClass: { confirmButton: 'btn btn-primary me-2', cancelButton: 'btn btn-label-secondary' }, buttonsStyling: false
    }).then(function (r) { return r.isConfirmed; });
  };

  // ---- Format VN ----
  tk.money = function (n) { return (Number(n) || 0).toLocaleString('vi-VN'); };
  // ---- Ô nhập TIỀN ----
  // Hiện có dấu phân cách nghìn kiểu Việt ("1.200.000"), nhưng GỬI ĐI số thô ("1200000").
  //
  // Vì sao không dùng type="number": nó không cho hiện dấu phân cách, mà bảng giá toàn số 7-9 chữ số
  // nên "1200000" và "12000000" nhìn gần như nhau — người nhập lệch một chữ số không nhận ra. Nó còn
  // kèm nút tăng/giảm vô nghĩa với tiền, và cuộn chuột trên ô đang focus là đổi giá trị lúc nào không hay.
  //
  // Vì sao tách hai ô: một ô không thể vừa hiện "1.200.000" vừa gửi "1200000". Ô nhìn thấy chỉ để
  // hiển thị, ô ẩn mang name thật nên giá trị gửi lên luôn parse được, không phụ thuộc lúc submit
  // người dùng đang focus hay không.
  // co: 'sm' (mặc định — dùng cho dòng trong bảng giá) hoặc '' (cỡ thường — dùng trong form).
  // Ép cứng cỡ nhỏ ở mọi nơi làm ô tiền thấp hơn hẳn ô số bên cạnh khi đặt trong form, nhìn so le.
  tk.oTien = function (ten, giaTri, chuMo, co) {
    var raw = (giaTri == null || giaTri === '') ? '' : String(giaTri);
    var nho = co === undefined || co === 'sm';
    return '<div class="input-group' + (nho ? ' input-group-sm' : '') + '">' +
      '<input type="text" inputmode="decimal" class="form-control' + (nho ? ' form-control-sm' : '') + ' tk-tien text-end"' +
      ' data-tien="' + ten + '" value="' + tk.escape(tk.tienDep(raw)) + '"' +
      ' placeholder="' + tk.escape(chuMo || '0') + '" autocomplete="off" />' +
      // Ô ẩn luôn có SỐ, kể cả khi ô nhìn thấy đang trống: cột giá ở CSDL không cho null, mà model
      // binding gặp chuỗi rỗng cho decimal là hỏng cả form và người dùng chỉ thấy "không lưu được"
      // mà không ô nào báo đỏ. Trống nghĩa là 0, và placeholder "0" nói đúng điều đó.
      '<input type="hidden" name="' + ten + '" value="' + tk.escape(raw === '' ? '0' : raw) + '" />' +
      '</div>';
  };

  /** "1200000" -> "1.200.000". Giữ nguyên phần thập phân người dùng đang gõ dở. */
  tk.tienDep = function (v) {
    if (v == null || v === '') { return ''; }
    var s = String(v).replace(/[^\d,.]/g, '').replace(/\./g, '');
    var p = s.split(',');
    var nguyen = (p[0] || '').replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    return p.length > 1 ? nguyen + ',' + p[1] : nguyen;
  };

  /** "1.200.000,5" -> "1200000.5" — dạng duy nhất model binding đọc được. */
  tk.tienTho = function (v) {
    if (v == null || v === '') { return ''; }
    return String(v).replace(/\./g, '').replace(',', '.').replace(/[^\d.\-]/g, '');
  };

  // Gắn MỘT lần cho cả trang: dòng bảng giá sinh động sau khi trang đã dựng, gắn theo từng ô thì ô
  // mới thêm không có người xử lý và âm thầm gửi lên chuỗi có dấu chấm.
  $(document).on('input', '.tk-tien', function () {
    var vt = this.selectionStart, dai = this.value.length;
    this.value = tk.tienDep(this.value);
    $(this).next('input[type="hidden"]').val(tk.tienTho(this.value) || '0');
    // Giữ con trỏ đứng yên khi số dấu chấm thay đổi, nếu không mỗi lần gõ con trỏ nhảy về cuối.
    try { this.setSelectionRange(vt + (this.value.length - dai), vt + (this.value.length - dai)); } catch (e) { /* ô không hỗ trợ */ }
  });

  // ---- Ô chọn GỌI SERVER (select2 ajax) ----
  // Dùng khi danh mục có thể lớn: nạp hết vào <option> thì hoặc trang nặng, hoặc phải đặt trần — mà
  // vượt trần là hỏng IM LẶNG (ô vẫn hiện bình thường, chỉ thiếu lựa chọn, người dùng tưởng dữ liệu
  // chưa có). Xem TranDanhMuc và TranDanhMucTests.
  //
  // opts: { url, placeholder, parent (offcanvas), min (số ký tự tối thiểu), extra }
  // extra nhận CẢ object tĩnh ({ type: 4 }) lẫn hàm (() => ({ providerId: $('#x').val() })). Bản đầu
  // chỉ nhận hàm, và truyền nhầm object thì không có gì báo lúc dựng trang — nó ném lỗi ở giữa lần
  // gõ đầu tiên, dropdown đứng im như thể server không trả về gì.
  tk.s2ajax = function (el, opts) {
    opts = opts || {};
    var $el = $(el);
    if (!$el.length || !$.fn.select2) { return $el; }

    return $el.select2({
      width: '100%',
      allowClear: true,
      placeholder: opts.placeholder || $el.data('placeholder') || 'Gõ để tìm…',
      dropdownParent: opts.parent ? $(opts.parent) : undefined,
      // minimumInputLength 0: MỞ ra là đã nạp sẵn TRANG ĐẦU (như ô có <option> sẵn) — không bắt gõ mới ra.
      // Gõ thì lọc như cũ. Có thể ép lại bằng opts.min nếu màn nào muốn buộc gõ.
      minimumInputLength: opts.min == null ? 0 : opts.min,
      ajax: {
        url: opts.url,
        dataType: 'json',
        delay: 250,                     // gõ nhanh không bắn một request mỗi phím
        cache: true,
        data: function (p) {
          var d = { q: p.term, page: p.page || 1 };   // gửi kèm trang để cuộn phân trang
          if (opts.extra) { $.extend(d, typeof opts.extra === 'function' ? opts.extra() : opts.extra); }
          return d;
        },
        // Endpoint trả {results, pagination:{more}} → cuộn vô hạn; trả {results} thường → hiện trang đầu (an toàn).
        processResults: function (d) {
          if (d && d.pagination) { return d; }
          return { results: (d && d.results) || [], pagination: { more: false } };
        }
      }
    });
  };

  /**
   * Nạp sẵn MỘT lựa chọn cho ô gọi server, dùng khi mở sửa bản ghi cũ.
   *
   * Ô gọi server lúc mở form thì rỗng vì chưa gõ gì. Không nạp sẵn thì người dùng thấy ô trống, bấm
   * Lưu là ghi null đè lên khoá ngoại — mất liên kết mà không có gì báo. Đây đúng kiểu mất dữ liệu
   * đã xảy ra ở form sửa chuyến đi và ở ô tỉnh thành.
   */
  tk.s2set = function (el, id, text) {
    var $el = $(el);
    if (!$el.length) { return; }
    $el.empty();
    if (id) { $el.append(new Option(text || String(id), id, true, true)); }
    $el.trigger('change');
  };


  /**
   * Nâng ô tiền do RAZOR dựng sẵn thành cặp "ô hiện thị + ô ẩn".
   *
   * Razor chỉ cần đánh dấu <input data-tien-auto name="Input.X" value="0">, phần còn lại làm ở đây —
   * để chỉ có MỘT bản hiện thực định dạng tiền. Viết lại markup đó bằng tay trong .cshtml là bản thứ
   * hai, và hai bản sẽ trôi lệch nhau.
   */
  tk.tienAuto = function (root) {
    $(root || document).find('input[data-tien-auto]').each(function () {
      var ten = this.name, gt = this.value, chuMo = this.getAttribute('placeholder') || '0';
      // GIỮ NGUYÊN cỡ của ô gốc: ô trong form khai form-control (cỡ thường) thì ô tiền thay vào
      // cũng phải cỡ thường, nếu không nó thấp hơn ô bên cạnh và cả hàng nhìn lệch.
      var co = this.classList.contains('form-control-sm') ? 'sm' : '';
      $(this).replaceWith(tk.oTien(ten, gt, chuMo, co));
    });
  };

  /**
   * Đồng bộ ô tiền NHÌN THẤY từ ô ẩn, gọi sau khi nạp dữ liệu vào form.
   *
   * Bộ điền của tk.form gán theo name, mà name nằm ở ô ẩn — nên nạp xong thì ô ẩn đúng còn ô người
   * dùng nhìn thấy vẫn trống. Mở sửa một vé có tiền mà ô hiện 0 thì người dùng gõ lại, và con số họ
   * gõ đè lên số cũ.
   */
  tk.tienSync = function (root) {
    $(root || document).find('input.tk-tien').each(function () {
      var $h = $(this).next('input[type="hidden"]');
      if ($h.length) { this.value = tk.tienDep($h.val()); }
    });
  };


  tk.escape = function (s) { return $('<div>').text(s == null ? '' : s).html(); };
  tk.trunc = function (s, max) { var e = tk.escape(s); return '<span class="d-inline-block text-truncate align-middle" style="max-width:' + (max || 180) + 'px" title="' + e + '">' + e + '</span>'; };

  // ---- DataTables server-side chuẩn (ngôn ngữ VN, fix width, dom Vuexy) ----
  tk.dtLanguage = {
    // Chỉ báo đang tải: vòng xoay theo màu thương hiệu + chữ, gói trong một viên thuốc nổi giữa
    // bảng. Bản mặc định của DataTables là ba chấm nảy trong khung trần — trông như trang chưa làm
    // xong. Markup để Ở ĐÂY (một chỗ) thay vì mỗi màn tự khai.
    processing: '<span class="tk-load tk-load-sm"><span class="tk-load-spin"></span><span>Đang tải<span class="tk-dots"></span></span></span>',
    search: 'Tìm:', lengthMenu: 'Hiện _MENU_ dòng',
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
      $btn = $('<button type="button" class="btn btn-icon btn-label-secondary position-relative" id="btn-adv" title="Lọc nâng cao" data-bs-toggle="tooltip">' +
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

  // ---- Thanh lọc: đồng nhất chiều cao + nút Tìm tự co còn icon khi hàng quá hẹp ----
  // Chạy TỰ ĐỘNG mọi trang (không cần sửa từng màn): gắn class .tk-filter-row cho hàng chứa #btn-search
  // (CSS ép mọi control cao 38px, canh giữa — hết thò thụt), bọc nhãn "Tìm" vào <span> để ẩn được, rồi
  // theo dõi bề rộng nhóm nút: hẹp quá thì bỏ nhãn, chừa icon (bám yêu cầu "ngắn thì mỗi icon").
  tk.filterBar = function () {
    var btn = document.getElementById('btn-search');
    if (!btn) { return; }

    var row = btn.closest('.row');
    if (row) { row.classList.add('tk-filter-row'); }

    if (!btn.querySelector('.tk-btn-label')) {
      var ic = btn.querySelector('i');
      var label = (btn.textContent || '').trim();
      btn.innerHTML = (ic ? ic.outerHTML : '') + '<span class="tk-btn-label">' + label + '</span>';
    }

    var group = btn.parentElement;
    if (!group || !window.ResizeObserver) { return; }
    // < ~150px: nhóm (Tìm + Đặt lại + Lọc) không đủ chỗ cho chữ → nút Tìm còn mỗi icon.
    var ro = new ResizeObserver(function () {
      btn.classList.toggle('tk-icon-only', group.clientWidth < 150);
    });
    ro.observe(group);
  };

  // ---- Ô chọn THỊ TRƯỜNG cha–con (cascading) dùng chung ----
  // Biến MỌI <select.tk-market> thành 2 ô select2 Cha→Con: chọn nhóm cha, ô con lọc theo cha. Giá trị
  // hiệu lực (con nếu chọn, không thì cha) ghi ngược vào ô GỐC (ẩn) nên filter/form KHÔNG phải đổi logic.
  // Cây nạp 1 lần từ ?handler=MarketTree (cache theo phiên). Chạy SAU cùng (setTimeout) để không đụng
  // select2 mà trang/tk.form vừa gắn. Bám yêu cầu owner: gọn, có tìm, phân cấp — thay danh sách phẳng dài.
  tk._marketTree = null;

  // Nạp cây thị trường 1 lần/lần-tải-trang (?handler=MarketTree đã cache server-side qua MarketDirectory).
  function getMarketTree(cb) {
    if (tk._marketTree) { return cb(tk._marketTree); }
    fetch(location.pathname + '?handler=MarketTree', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
      .then(function (r) { return r.ok ? r.json() : []; })
      .then(function (d) { tk._marketTree = d || []; cb(tk._marketTree); })
      .catch(function () { cb([]); });
  }

  // Bộ lọc THỊ TRƯỜNG dạng CÂY multi-checkbox cho MÀN DANH SÁCH (khác FORM: form là 1 select cha–con).
  // Check được NHIỀU; check cha → check hết con; check vài con → cha hiện "một phần" (indeterminate).
  // Ghi TẬP id đã chọn (cả cha lẫn con đang tick) vào ô hidden (nối bằng dấu phẩy) để tk.grid gửi kèm
  // → backend lọc IN. opts: { mount:'#id', hidden:'#f-marketTypeIds', onChange:fn(tải lại khi đóng panel) }
  tk.marketFilter = function (opts) {
    if (!window.jQuery) { return; }
    var $mount = jQuery(opts.mount), $hidden = jQuery(opts.hidden);
    if (!$mount.length || !$hidden.length) { return; }
    function esc(s) {
      return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) {
        return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c];
      });
    }

    getMarketTree(function (tree) {
      var roots = [], kids = {};
      tree.forEach(function (m) {
        if (m.parentId) { (kids[m.parentId] = kids[m.parentId] || []).push(m); } else { roots.push(m); }
      });
      var cmp = function (a, b) { return (a.sort - b.sort) || String(a.name).localeCompare(b.name); };
      roots.sort(cmp); Object.keys(kids).forEach(function (k) { kids[k].sort(cmp); });

      var html = '<div class="dropdown tk-mktf w-100">' +
        '<button type="button" class="btn btn-label-secondary w-100 d-flex align-items-center justify-content-between dropdown-toggle" ' +
        'data-bs-toggle="dropdown" data-bs-auto-close="outside" aria-expanded="false">' +
        '<span class="tk-mktf-lbl text-truncate">Thị trường</span></button>' +
        '<div class="dropdown-menu p-2 tk-mktf-menu" style="max-height:320px;overflow:auto;min-width:250px">';
      roots.forEach(function (p) {
        html += '<div class="form-check mb-1">' +
          '<input class="form-check-input tk-mktf-p" type="checkbox" value="' + p.id + '" id="mktf-' + p.id + '">' +
          '<label class="form-check-label fw-medium" for="mktf-' + p.id + '">' + esc(p.name) + '</label></div>';
        (kids[p.id] || []).forEach(function (c) {
          html += '<div class="form-check mb-1 ms-3">' +
            '<input class="form-check-input tk-mktf-c" type="checkbox" value="' + c.id + '" data-parent="' + p.id + '" id="mktf-' + c.id + '">' +
            '<label class="form-check-label" for="mktf-' + c.id + '">' + esc(c.name) + '</label></div>';
        });
      });
      html += '</div></div>';
      $mount.html(html);

      var $menu = $mount.find('.tk-mktf-menu'), $lbl = $mount.find('.tk-mktf-lbl');

      function apply() {
        var ids = [];
        $menu.find('input:checked').each(function () { ids.push(this.value); });
        $hidden.val(ids.join(','));
        // Nhãn đếm: cha tick đủ tính 1; con tick lẻ (cha chưa đủ) tính từng cái — không đếm trùng.
        var n = 0;
        $menu.find('.tk-mktf-p').each(function () {
          if (this.checked) { n++; }
          else { n += $menu.find('.tk-mktf-c[data-parent="' + this.value + '"]:checked').length; }
        });
        $lbl.text(n ? ('Thị trường (' + n + ')') : 'Thị trường');
      }
      function syncParent(pid) {
        var $p = $menu.find('#mktf-' + pid), $cs = $menu.find('.tk-mktf-c[data-parent="' + pid + '"]');
        var total = $cs.length, on = $cs.filter(':checked').length;
        if (!total) { return; }
        $p.prop('checked', on === total).prop('indeterminate', on > 0 && on < total);
      }

      $menu.on('change', '.tk-mktf-p', function () {
        this.indeterminate = false;
        $menu.find('.tk-mktf-c[data-parent="' + this.value + '"]').prop('checked', this.checked);
        apply();
      });
      $menu.on('change', '.tk-mktf-c', function () { syncParent(this.getAttribute('data-parent')); apply(); });
      $menu.on('click', function (e) { e.stopPropagation(); });

      if (opts.onChange) {
        var last = '';
        $mount.find('.dropdown').on('hidden.bs.dropdown', function () {
          var cur = $hidden.val() || '';
          if (cur !== last) { last = cur; opts.onChange(); }
        });
      }

      function clearAll() {
        $menu.find('input').prop('checked', false).prop('indeterminate', false);
        $hidden.val(''); $lbl.text('Thị trường');
      }
      // Nút "Đặt lại" của thanh lọc chuẩn chỉ xoá VALUE ô hidden — phải xoá cả cây (tick) ở đây.
      var rst = document.getElementById('btn-reset');
      if (rst) { rst.addEventListener('click', clearAll); }
    });
  };

  tk.marketCascade = function () {
    // ĐÃ GỠ: owner chốt "chỉ 1 dòng search market là đủ" (bỏ qua phân Inbound/Outbound ở giao diện tìm).
    // Không dựng cascade cha–con nữa → ô thị trường giữ select2 tìm kiếm 1 ô (đơn giản + hết bị bóp cụt).
    // DATA thị trường vẫn giữ nguyên (kể cả Inbound/Outbound). Để hàm no-op cho gọn, khỏi gỡ class tk-market khắp nơi.
    return;
    // eslint-disable-next-line no-unreachable
    var list = document.querySelectorAll('select.tk-market:not([data-cascaded])');
    if (!list.length || !window.jQuery) { return; }

    getTree(function (tree) {
      var byId = {}, roots = [], kids = {};
      tree.forEach(function (m) { byId[m.id] = m; });
      tree.forEach(function (m) {
        if (m.parentId) { (kids[m.parentId] = kids[m.parentId] || []).push(m); }
        else { roots.push(m); }
      });
      var cmp = function (a, b) { return (a.sort - b.sort) || String(a.name).localeCompare(b.name); };
      roots.sort(cmp);
      Object.keys(kids).forEach(function (k) { kids[k].sort(cmp); });
      list.forEach(function (orig) { build(orig, byId, roots, kids); });
    });

    function getTree(cb) {
      // Cache theo LẦN TẢI TRANG (biến RAM) — đủ để nhiều ô trên cùng trang khỏi tải lại; KHÔNG dùng
      // sessionStorage vì danh mục có thể đổi, cache dính gây hiện cây cũ tới khi đóng tab.
      if (tk._marketTree) { return cb(tk._marketTree); }
      fetch(location.pathname + '?handler=MarketTree', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function (r) { return r.ok ? r.json() : []; })
        .then(function (d) { tk._marketTree = d || []; cb(tk._marketTree); })
        .catch(function () { cb([]); });
    }

    function build(orig, byId, roots, kids) {
      orig.setAttribute('data-cascaded', '1');
      var $orig = $(orig);
      var ph = $orig.data('placeholder') || 'Thị trường';
      var inOc = $orig.closest('.offcanvas');
      var busy = false;

      // Ô gốc đã bị trang/tk.form select2-hoá → gỡ bỏ, ẩn đi, GIỮ làm nơi chứa giá trị (filter/form đọc ô này).
      try { if ($orig.hasClass('select2-hidden-accessible')) { $orig.select2('destroy'); } } catch (e) {}
      $orig.addClass('d-none');

      var $cha = $('<select class="form-select"><option value=""></option></select>');
      var $con = $('<select class="form-select"></select>');
      roots.forEach(function (r) { $cha.append(new Option(r.name, r.id)); });
      $orig.after($('<div class="tk-market-cascade"></div>').append($cha, $con));

      function fillCon(chaVal) {
        $con.empty();
        var ks = (chaVal && kids[chaVal]) || [];
        if (!chaVal || ks.length === 0) { $con.append(new Option('—', '')).prop('disabled', true); }
        else {
          $con.prop('disabled', false).append(new Option('Cả nhóm', '')); // '' = dùng chính cha
          ks.forEach(function (k) { $con.append(new Option(k.name, k.id)); });
        }
      }
      function setFromOrig() { // ô gốc → Cha/Con (mở form sửa / reset / vào trang kèm tham số)
        var v = orig.value, n = v && byId[v];
        if (n && n.parentId) { $cha.val(n.parentId); fillCon(n.parentId); $con.val(v); }
        else if (n) { $cha.val(v); fillCon(v); }
        else { $cha.val(''); fillCon(''); }
      }
      function pull() { if (busy) { return; } busy = true; setFromOrig(); $cha.trigger('change.select2'); $con.trigger('change.select2'); busy = false; }
      function sync() { // Cha/Con → giá trị hiệu lực về ô gốc + kích change (filter reload / form value)
        if (busy) { return; } busy = true;
        var eff = $con.val() || $cha.val() || '';
        if (orig.value !== eff) { orig.value = eff; $orig.trigger('change'); }
        busy = false;
      }

      setFromOrig(); // dựng trạng thái ban đầu TRƯỚC khi select2-hoá
      var s2 = { width: '100%', allowClear: true };
      if (inOc.length) { s2.dropdownParent = inOc; }
      $cha.select2($.extend({}, s2, { placeholder: ph }));
      $con.select2($.extend({}, s2, { placeholder: 'Nhóm con' }));

      $orig.on('change', pull);
      $cha.on('change', function () { if (busy) { return; } fillCon($cha.val()); $con.trigger('change.select2'); sync(); });
      $con.on('change', function () { if (busy) { return; } sync(); });
    }
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

  // ---- Gò một khối cho vừa phần màn hình còn lại ----
  // Dùng cho khung KHÔNG phải Tabulator (bảng kanban, ma trận quỹ phòng…): cuộn NGAY TRONG khối
  // thay vì kéo dài trang rồi sinh thanh cuộn ngoài — cùng luật với lưới danh sách.
  // Gọi lại sau mỗi lần vẽ; tự bám sự kiện resize.
  tk.fitBox = function (el, minHeight) {
    el = typeof el === 'string' ? document.querySelector(el) : el;
    if (!el) { return; }
    function fit() {
      if (!el.offsetParent) { return; }           // đang ẩn thì đo ra 0, để nguyên
      var top = el.getBoundingClientRect().top;
      el.style.maxHeight = Math.max(minHeight || 260, Math.round(window.innerHeight - top - 24)) + 'px';
      el.style.overflowY = 'auto';
      // Còn dư vài pixel là trang đã có thanh cuộn — trừ nốt phần dư đó.
      var doc = document.documentElement;
      var over = doc.scrollHeight - doc.clientHeight;
      if (over > 2) {
        el.style.maxHeight = Math.max(minHeight || 260, parseInt(el.style.maxHeight, 10) - over) + 'px';
      }
    }
    if (!el.__tkFit) { el.__tkFit = true; window.addEventListener('resize', fit); }
    fit();
  };

  // ---- Bảng Kanban dùng CHUNG (jKanban, theo đúng khuôn app-kanban của theme) ----
  // Mỗi cột nạp RIÊNG một trang + nút "Tải thêm" — không đổ cả bảng ra client.
  // opts:
  //   pane        '#pane-kanban'          — khối chứa (có sẵn .kanban-wrapper bên trong)
  //   columns     [{ id, name, color }]   — cột hiển thị
  //   url         function(colId, page, size) → chuỗi URL handler
  //   card        function(row) → { badges:[{text,color}], title, meta, foot, footIcon, footTone, who }
  //   moveUrl     '?handler=Move'         — POST { id, to } khi kéo sang cột khác
  //   onMoved     function()              — chạy sau khi kéo–thả thành công
  //   onCardClick function(row)
  //   width       '17rem'
  tk.kanban = function (opts) {
    var pane = document.querySelector(opts.pane || '#pane-kanban');
    if (!pane) { return null; }
    var wrapEl = pane.querySelector('.kanban-wrapper');
    var size = opts.size || 15;
    var board = null, pages = {}, rows = {};

    function esc(s) { return tk.escape(s); }

    // Tiêu đề cột: chấm màu + tên + số đếm. KHÔNG tô nền cả cột như bản trước — theme để
    // đầu cột phẳng, tô nền cả khối làm bảng trông rối và nặng.
    function headHtml(col, total) {
      return '<span class="tk-kb-dot bg-' + esc(col.color || 'secondary') + '"></span>' +
        esc(col.name) + '<span class="badge bg-label-secondary ms-2 fw-medium">' + total + '</span>';
    }

    // Thẻ theo đúng khuôn thẻ của theme: hàng nhãn → tiêu đề .kanban-text → dòng phụ → chân thẻ.
    function cardHtml(row) {
      var c = opts.card(row) || {};
      var badges = (c.badges || []).filter(Boolean).map(function (b) {
        return '<span class="badge rounded-pill bg-label-' + esc(b.color || 'secondary') + '">' + esc(b.text) + '</span>';
      }).join(' ');
      var foot = '';
      if (c.foot) {
        foot += '<span class="d-flex align-items-center ' + (c.footTone ? 'text-' + esc(c.footTone) : 'text-muted') + '">' +
          (c.footIcon ? '<i class="' + esc(c.footIcon) + ' ti-xs me-1"></i>' : '') + esc(c.foot) + '</span>';
      }
      if (c.who) {
        foot += '<span class="avatar avatar-xs" title="' + esc(c.who) + '">' +
          '<span class="avatar-initial rounded-circle bg-label-' + esc(tk.g ? tk.g.toneOf(c.who) : 'primary') + '">' +
          esc(tk.g ? tk.g.initials(c.who) : c.who.slice(0, 2)) + '</span></span>';
      }
      return (badges ? '<div class="item-badges mb-2">' + badges + '</div>' : '') +
        '<span class="kanban-text">' + esc(c.title || '') + '</span>' +
        (c.meta ? '<div class="tk-kb-meta">' + esc(c.meta) + '</div>' : '') +
        (foot ? '<div class="tk-kb-foot">' + foot + '</div>' : '');
    }

    function colEl(id) { return pane.querySelector('.kanban-board[data-id="kb-' + id + '"]'); }

    function syncMore(col, res) {
      var el = colEl(col.id); if (!el) { return; }
      var head = el.querySelector('.kanban-title-board');
      if (head) { head.innerHTML = headHtml(col, res.total); }
      var btn = el.querySelector('.kb-more');
      if (!res.hasMore) { if (btn) { btn.remove(); } return; }
      if (!btn) {
        btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-label-primary w-100 kb-more';
        btn.textContent = 'Tải thêm';
        btn.addEventListener('click', function () { loadMore(col); });
        el.querySelector('.kanban-drag').appendChild(btn);
      } else {
        el.querySelector('.kanban-drag').appendChild(btn);   // luôn nằm CUỐI cột
      }
    }

    function loadMore(col) {
      var next = (pages[col.id] || 1) + 1;
      $.getJSON(opts.url(col.id, next, size), function (res) {
        pages[col.id] = next;
        res.cards.forEach(function (c) {
          rows[c.id] = c;
          board.addElement('kb-' + col.id, { id: c.id, title: cardHtml(c) });
        });
        syncMore(col, res);
      });
    }

    function render() {
      $(wrapEl).empty();
      pages = {}; rows = {}; board = null;
      var reqs = opts.columns.map(function (col) { return $.getJSON(opts.url(col.id, 1, size)); });
      $.when.apply($, reqs).done(function () {
        var results = opts.columns.length === 1 ? [arguments] : Array.prototype.slice.call(arguments);
        var boards = opts.columns.map(function (col, i) {
          var res = results[i][0];
          pages[col.id] = 1;
          res.cards.forEach(function (c) { rows[c.id] = c; });
          return {
            id: 'kb-' + col.id,
            title: headHtml(col, res.total),
            item: res.cards.map(function (c) { return { id: c.id, title: cardHtml(c) }; })
          };
        });

        board = new jKanban({
          element: (opts.pane || '#pane-kanban') + ' .kanban-wrapper',
          gutter: '12px', widthBoard: opts.width || '17rem',
          dragItems: true, dragBoards: false, boards: boards,
          dropEl: function (el, target) {
            var id = el.getAttribute('data-eid');
            var to = target.parentElement.getAttribute('data-id').replace('kb-', '');
            var fd = new FormData(); fd.append('id', id); fd.append('to', to);
            tk.post(opts.moveUrl, fd).then(function (r) {
              if (r && r.isSuccess) { tk.toast(r.message); if (opts.onMoved) { opts.onMoved(); } render(); }
              // Server từ chối thì vẽ lại để thẻ về đúng cột cũ — đừng để màn hình nói dối.
              else { tk.error((r && r.message) || 'Không chuyển được.'); render(); }
            });
          },
          click: function (el) {
            var row = rows[el.getAttribute('data-eid')];
            if (row && opts.onCardClick) { opts.onCardClick(row); }
          }
        });

        opts.columns.forEach(function (col, i) { syncMore(col, results[i][0]); });
        fit();
      });
    }

    // Khung cao đúng phần màn hình còn lại; cuộn dọc nằm TRONG từng cột (xem tourkit.css).
    // Theme đặt sẵn height: calc(100vh - 12rem) — ở đây khung nằm sâu trong trang nên tính lại,
    // nếu không trang mọc thêm một thanh cuộn nữa.
    function fit() {
      if (!wrapEl.offsetParent) { return; }
      var top = wrapEl.getBoundingClientRect().top;
      var h = Math.max(320, Math.round(window.innerHeight - top - 24));
      wrapEl.style.height = h + 'px';
      var doc = document.documentElement;
      var over = doc.scrollHeight - doc.clientHeight;
      if (over > 2) { wrapEl.style.height = Math.max(320, h - over) + 'px'; }
    }
    if (!wrapEl.__tkFit) { wrapEl.__tkFit = true; window.addEventListener('resize', fit); }

    return {
      render: render,
      refit: fit,
      isBuilt: function () { return !!board; }
    };
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
      tk.tienAuto($form);
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
        // ':hidden' để bỏ qua ô trong khối đang ẩn. Nhưng CHÍNH hai thứ ở trên vừa giấu ô gốc đi:
        // flatpickr altInput chuyển input gốc thành hidden, tk.rte giấu textarea sau ô soạn thảo.
        // Không trừ chúng ra thì luật `required` khai cho ô ngày và ô nội dung được nạp đầy đủ mà
        // không bao giờ chạy — form trông có kiểm tra, thực tế lưu được bản ghi rỗng.
        ignore: ':hidden:not(.tk-date):not(.tk-rte-field)',
        errorElement: 'span', errorClass: 'text-danger d-block small mt-1',
        rules: opts.rules || {}, messages: opts.messages || {},
        highlight: function (el) { $(el).addClass('is-invalid'); }, unhighlight: function (el) { $(el).removeClass('is-invalid'); }
      });
    }

    // Bản ghi cũ có thể mang giá trị KHÔNG còn trong danh mục (dữ liệu gõ tay trước đây, hoặc danh
    // mục đã sửa/xoá mục đó). Select không có option tương ứng thì ô hiện trống và lần Lưu kế tiếp
    // âm thầm XOÁ MẤT giá trị cũ — không báo lỗi, không ai biết. Giữ lại bằng một option tạm.
    function keepLegacyOption(el, v) {
      if (!el || el.tagName !== 'SELECT' || v == null || v === '') { return; }
      var val = String(v);
      for (var i = 0; i < el.options.length; i++) { if (el.options[i].value === val) { return; } }
      el.add(new Option(val + ' (ngoài danh mục)', val), el.options[1] || null);
    }

    function setField(name, v) {
      var el = $form.find('[name="' + name + '"]')[0];
      if (!el) { return; }
      if (el._flatpickr) { v ? el._flatpickr.setDate(v, true) : el._flatpickr.clear(); return; }
      if ($(el).hasClass('tk-s2') || $(el).hasClass('tk-s2-tags')) {
        // tk-s2-tags cho phép giá trị tự do sẵn nên không cần vá; tk-s2 thì cần.
        if (!$(el).hasClass('tk-s2-tags')) { keepLegacyOption(el, v); }
        $(el).val(v == null || v === '' ? null : v).trigger('change');
        return;
      }
      if (el.type === 'checkbox') { el.checked = !!v; return; }
      keepLegacyOption(el, v);
      el.value = (v == null) ? '' : v;
      // Giá trị RỖNG gán vào <select> không có mục rỗng làm ô trắng trơn, người dùng tưởng chưa chọn
      // được gì. Rơi về mục đầu tiên — cũng chính là mặc định của server. Chỉ áp cho giá trị rỗng:
      // giá trị KHÁC rỗng mà không khớp đã được keepLegacyOption giữ lại ở trên.
      if (el.tagName === 'SELECT' && el.selectedIndex === -1) { el.selectedIndex = 0; }
      // Ô soạn thảo giữ nội dung trong Quill, gán value cho textarea ẩn là chưa đủ.
      if (el._quill) { tk.rte.set(el, el.value); }
    }

    /**
     * Giá trị của một ô khi THÊM MỚI: lấy mặc định ghi trong markup, không phải chuỗi rỗng.
     *
     * Xoá trắng ô số làm form gửi lên "" và .NET không ghép được vào int — trả về "The value ''
     * is invalid." và KHÔNG tạo được bản ghi nào. Lỗi này giết luồng thêm mới của 7 màn danh mục
     * mà không ai biết, vì sửa bản ghi có sẵn thì vẫn chạy bình thường.
     *
     * Đây cũng chính là ngữ nghĩa của form.reset() trong HTML: quay về defaultValue.
     */
    function macDinh(el) {
      if (el.type === 'checkbox') { return el.defaultChecked; }
      return el.defaultValue || '';
    }

    // Bản ghi đang mở, giữ lại để beforeSave so được GIÁ TRỊ CŨ với giá trị người dùng vừa gõ.
    // Không giữ thì hook chỉ thấy trạng thái mới và không biết người dùng có đổi gì hay không —
    // sinh ra cảnh báo cả khi mở form rồi bấm Lưu mà không sửa gì. null = đang thêm mới.
    var dangMo = null;

    function open(data) {
      dangMo = data || null;
      if ($.fn.validate) { $form.validate().resetForm(); }
      // Điền: mọi [name] có dạng "Input.X"/"Id" → data[camelCase]
      $form.find('[name]').each(function () {
        var name = this.getAttribute('name');
        var key = name.replace(/^Input\./, '');
        key = key.charAt(0).toLowerCase() + key.slice(1);
        setField(name, data ? data[key] : macDinh(this));
      });
      // Ô tiền: name nằm ở ô ẩn nên vòng điền ở trên chỉ chạm được ô ẩn. Không đồng bộ thì mở sửa
      // một bản ghi có tiền mà ô người dùng nhìn thấy vẫn trống — họ gõ lại và đè lên số cũ.
      tk.tienSync($form);
      if (opts.fill) { opts.fill($form, data); }
      var lbl = ocEl.querySelector('.offcanvas-title');
      // "Sửa" chỉ khi có ID (bản ghi đã tồn tại). Mở form với dữ liệu ĐIỀN SẴN nhưng KHÔNG id (vd AI
      // bóc thông tin để tạo mới) vẫn là "Thêm" — bám đúng logic Mã-readonly ở trên (data && data.id).
      if (lbl && opts.title) { lbl.textContent = ((data && data.id) ? ('Sửa ' + opts.title) : ('Thêm ' + opts.title)); }
      if (opts.afterOpen) { opts.afterOpen(data); }
      bootstrap.Offcanvas.getOrCreateInstance(ocEl).show();
    }

    function gui(formEl) {
      tk.overlay.show('Đang lưu…');
      tk.post($form.attr('data-action') || opts.saveUrl, new FormData(formEl)).then(function (res) {
        if (res && res.isSuccess) {
          bootstrap.Offcanvas.getInstance(ocEl).hide();
          var dt = opts.table && $.fn.DataTable.isDataTable(opts.table) ? $(opts.table).DataTable() : null;
          var serverSide = dt && dt.settings()[0].oInit.serverSide;
          if (serverSide) { dt.ajax.reload(null, false); tk.toast(res.message || 'Đã lưu.'); }
          // Màn dùng tk.grid (Tabulator) không có DataTable — nạp lại lưới tại chỗ, đừng tải lại cả trang.
          else if (window.tkGridReload) { window.tkGridReload(); tk.toast(res.message || 'Đã lưu.'); }
          else { location.reload(); }   // client-side: re-render dòng từ server
        } else { tk.error((res && (res.message || res.detail || res.title)) || 'Lưu thất bại.'); }
      }).finally(function () { tk.overlay.hide(); });
    }

    /**
     * beforeSave($form, banGhiCu) — chặn cuối trước khi gửi.
     *
     * Trả về false (hoặc Promise trả false) thì DỪNG, giữ nguyên form đang mở để người dùng sửa
     * tiếp. Trả về bất cứ thứ gì khác thì lưu. Nhận cả Promise nên hook hỏi lại người dùng bằng hộp
     * xác nhận được — đó là lý do có hook này.
     *
     * banGhiCu là dòng lúc mở form (null khi thêm mới), để hook so được cũ với mới.
     */
    $form.on('submit', function (e) {
      e.preventDefault();
      if ($.fn.validate && !$form.valid()) { return; }
      var formEl = this;
      if (!opts.beforeSave) { gui(formEl); return; }
      Promise.resolve(opts.beforeSave($form, dangMo)).then(function (ok) {
        if (ok !== false) { gui(formEl); }
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

  // ---- Khoảng thời gian tương đối kiểu VN ("5 phút trước", "Hôm qua 09:30") ----
  tk.ago = function (iso) {
    var d = new Date(iso);
    if (isNaN(d)) { return ''; }
    var s = Math.floor((Date.now() - d.getTime()) / 1000);
    if (s < 60) { return 'Vừa xong'; }
    if (s < 3600) { return Math.floor(s / 60) + ' phút trước'; }
    if (s < 86400) { return Math.floor(s / 3600) + ' giờ trước'; }
    var hm = ('0' + d.getHours()).slice(-2) + ':' + ('0' + d.getMinutes()).slice(-2);
    if (s < 172800) { return 'Hôm qua ' + hm; }
    return ('0' + d.getDate()).slice(-2) + '/' + ('0' + (d.getMonth() + 1)).slice(-2) + ' ' + hm;
  };

  // ---- Chuông thông báo trên navbar ----
  // Dữ liệu lấy từ /thong-bao/chuong (xem Pages/Notifications/Bell.cshtml.cs). Nạp một lần lúc mở
  // trang để hiện số chưa đọc, và nạp lại mỗi lần mở dropdown — không polling (thông báo ở đây là
  // việc nội bộ, không cần thời gian thực; polling nền tốn truy vấn cho mọi tab đang mở).
  var BELL_ICON = {
    approval: ['ti-file-check', 'warning'],
    task: ['ti-checklist', 'info'],
    marketing: ['ti-speakerphone', 'success'],
    system: ['ti-bell', 'primary']
  };

  tk.bell = function () {
    var root = document.getElementById('tk-bell');
    if (!root) { return; }
    var list = root.querySelector('[data-role="list"]');
    var count = root.querySelector('[data-role="count"]');
    var label = root.querySelector('[data-role="unread-label"]');
    var loaded = false;

    function setCount(n) {
      n = Number(n) || 0;
      count.textContent = n > 99 ? '99+' : n;
      count.classList.toggle('d-none', n === 0);
      label.textContent = n + ' chưa đọc';
      label.classList.toggle('d-none', n === 0);
    }

    function row(n) {
      var ic = BELL_ICON[n.type] || BELL_ICON.system;
      var body = n.message ? '<p class="mb-1 small text-muted text-truncate">' + tk.escape(n.message) + '</p>' : '';
      return '<li class="list-group-item list-group-item-action dropdown-notifications-item' +
               (n.isRead ? ' marked-as-read' : '') + '" data-id="' + n.id + '"' +
               (n.linkUrl ? ' data-link="' + tk.escape(n.linkUrl) + '"' : '') + '>' +
               '<div class="d-flex align-items-start">' +
                 '<div class="flex-shrink-0 me-3"><div class="avatar">' +
                   '<span class="avatar-initial rounded-circle bg-label-' + ic[1] + '"><i class="ti ' + ic[0] + '"></i></span>' +
                 '</div></div>' +
                 '<div class="flex-grow-1 overflow-hidden">' +
                   '<h6 class="mb-0 text-truncate">' + tk.escape(n.title) + '</h6>' + body +
                   '<small class="text-muted">' + tk.ago(n.createdAt) + '</small>' +
                 '</div>' +
                 '<div class="flex-shrink-0 dropdown-notifications-actions">' +
                   '<a href="javascript:void(0)" class="dropdown-notifications-read" title="Đánh dấu đã đọc">' +
                     '<span class="badge badge-dot"></span></a>' +
                 '</div>' +
               '</div></li>';
    }

    function load() {
      return fetch('/thong-bao/chuong', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function (r) { return r.ok ? r.json() : null; })
        .then(function (d) {
          if (!d) { return; }
          loaded = true;
          setCount(d.unread);
          list.innerHTML = d.items.length
            ? d.items.map(row).join('')
            : '<li class="list-group-item text-center text-muted py-4">Chưa có thông báo nào.</li>';
        })
        .catch(function () { /* mất mạng thì giữ nguyên trạng thái cũ, không phá giao diện */ });
    }

    function markRead(id, li) {
      if (!li || li.classList.contains('marked-as-read')) { return; }
      li.classList.add('marked-as-read');
      var fd = new FormData(); fd.append('id', id);
      tk.post('/thong-bao/chuong?handler=Read', fd).then(function (r) {
        if (r && r.isSuccess && r.data) { setCount(r.data.unread); }
      });
    }

    // Mở dropdown → nạp lại cho tươi (lần đầu đã nạp sẵn để có số trên chuông).
    root.addEventListener('show.bs.dropdown', function () { if (loaded) { load(); } });

    list.addEventListener('click', function (e) {
      var li = e.target.closest('.dropdown-notifications-item');
      if (!li) { return; }
      if (e.target.closest('.dropdown-notifications-read')) { markRead(li.dataset.id, li); return; }
      // Kích cả dòng: đánh dấu đã đọc rồi đi tới nơi liên quan (nếu có link).
      markRead(li.dataset.id, li);
      if (li.dataset.link) { window.location.href = li.dataset.link; }
    });

    var all = root.querySelector('[data-role="read-all"]');
    if (all) {
      all.addEventListener('click', function () {
        tk.post('/thong-bao/chuong?handler=ReadAll', new FormData()).then(function (r) {
          if (r && r.isSuccess) {
            setCount(0);
            list.querySelectorAll('.dropdown-notifications-item').forEach(function (li) {
              li.classList.add('marked-as-read');
            });
          }
        });
      });
    }

    load();
  };

  window.tk = tk;

  // Gộp date-range TỰ ĐỘNG trên mọi trang. Đăng ký ở đây (tk.js nạp trước) nên callback này chạy
  // TRƯỚC $(function) của từng trang → vô hiệu ô gốc xong mới tới lúc trang flatpickr('.tk-datef').
  $(function () { tk.dateRanges(); tk.filterBar(); tk.bell(); setTimeout(tk.marketCascade, 0); });
})();
