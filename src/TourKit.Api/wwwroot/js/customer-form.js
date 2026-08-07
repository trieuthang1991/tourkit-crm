/* Offcanvas thêm/sửa khách hàng — theo docs/UI-CONVENTIONS.md:
   Select2 (search + tags multi) · flatpickr (date) · jQuery Validate · section Doanh nghiệp động · lưu AJAX. */
(function () {
  'use strict';

  var $ = window.jQuery;

  function token() {
    var el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
  }

  // Giá trị đang lưu của bản ghi có thể KHÔNG còn trong danh mục (dữ liệu cũ gõ tay, hoặc danh mục
  // đã sửa). Select không có option đó thì select2 hiện trống và lần Lưu kế tiếp âm thầm XOÁ MẤT
  // giá trị cũ. Thêm tạm một option cho đúng giá trị đang có.
  function keepLegacyOption(el, v) {
    if (!el || el.tagName !== 'SELECT' || !v) { return; }
    if ([].some.call(el.options, function (o) { return o.value === v; })) { return; }
    el.add(new Option(v + ' (ngoài danh mục)', v), el.options[1] || null);
  }

  // Set giá trị: flatpickr -> setDate; select2 -> jQuery val+trigger; input thường -> .value.
  function setVal(name, v) {
    var el = document.querySelector('#customerForm [name="' + name + '"]');
    if (!el) { return; }
    if (el._flatpickr) {
      if (v) { el._flatpickr.setDate(v, true); } else { el._flatpickr.clear(); }
      return;
    }
    if ($ && (el.classList.contains('oc-select2') || el.classList.contains('oc-select2-tags'))) {
      keepLegacyOption(el, v);
      $(el).val(v === null || v === undefined || v === '' ? null : v).trigger('change');
      return;
    }
    keepLegacyOption(el, v);
    el.value = (v === null || v === undefined) ? '' : v;
  }

  function initSelect2() {
    if (!$ || !$.fn || !$.fn.select2) { return; }
    var parent = $('#ocEditCustomer');
    $('#customerForm .oc-select2').each(function () {
      $(this).select2({ dropdownParent: parent, width: '100%', allowClear: true, placeholder: $(this).data('placeholder') || '' });
    });
    $('#customerForm .oc-select2-tags').each(function () {
      $(this).select2({ dropdownParent: parent, width: '100%', multiple: true, placeholder: $(this).data('placeholder') || '' });
    });
  }

  function initPickers() {
    if (!window.flatpickr) { return; }
    document.querySelectorAll('#customerForm .oc-date').forEach(function (el) {
      if (!el._flatpickr) {
        flatpickr(el, { altInput: true, altFormat: 'd/m/Y', dateFormat: 'Y-m-d', allowInput: true });
      }
    });
  }

  function initValidate() {
    if (!$ || !$.fn || !$.fn.validate) { return; }
    $('#customerForm').validate({
      ignore: ':hidden',   // field trong section ẩn (Doanh nghiệp) không bị bắt lỗi
      errorElement: 'span',
      errorClass: 'text-danger d-block small mt-1',
      rules: {
        'Input.FullName': { required: true },
        'Input.Phone': { required: true },
        'Input.UnitName': { required: true },   // chỉ hiệu lực khi section Doanh nghiệp hiện (không bị ignore)
        'Input.Email': { email: true }
      },
      messages: {
        'Input.FullName': { required: 'Bắt buộc nhập họ tên' },
        'Input.Phone': { required: 'Bắt buộc nhập số điện thoại' },
        'Input.UnitName': { required: 'Bắt buộc nhập tên đơn vị' },
        'Input.Email': { email: 'Email không hợp lệ' }
      },
      highlight: function (el) { $(el).addClass('is-invalid'); },
      unhighlight: function (el) { $(el).removeClass('is-invalid'); }
    });
  }

  function syncBusiness() {
    var sel = document.getElementById('oc-type');
    if (!sel) { return; }
    var opt = sel.options[sel.selectedIndex];
    var isOrg = opt && opt.getAttribute('data-business') === 'true';
    var box = document.getElementById('oc-business');
    if (box) { box.classList.toggle('d-none', !isOrg); }   // ẩn -> jQuery Validate bỏ qua UnitName
  }

  // data = null -> thêm mới; data = {...} -> sửa.
  window.openCustomerOffcanvas = function (data) {
    if ($) { $('#customerForm').validate().resetForm(); }
    setVal('Id', data ? data.id : '');
    setVal('Input.CustomerType', data ? (data.customerType || 0) : 0);
    setVal('Input.UnitName', data ? data.unitName : '');
    setVal('Input.TaxCode', data ? data.taxCode : '');
    setVal('Input.FullName', data ? data.fullName : '');
    setVal('Input.Phone', data ? data.phone : '');
    setVal('Input.Email', data ? data.email : '');
    setVal('Input.Gender', data ? data.gender : '');
    setVal('Input.DateOfBirth', data ? data.dateOfBirth : '');
    setVal('Input.Source', data ? data.source : '');
    setVal('Input.Tags', data && data.tags ? data.tags : []);
    setVal('Input.MarketGroup', data ? data.marketGroup : '');
    setVal('Input.CollaboratorName', data ? data.collaboratorName : '');
    setVal('Input.City', data ? data.city : '');
    setVal('Input.Address', data ? data.address : '');
    setVal('Input.IdCardNumber', data ? data.idCardNumber : '');
    setVal('Input.PassportExpiry', data ? data.passportExpiry : '');
    setVal('Input.Note', data ? data.note : '');
    var lbl = document.getElementById('ocEditLabel');
    if (lbl) { lbl.textContent = data ? 'Sửa khách hàng' : 'Thêm khách hàng'; }
    syncBusiness();
    bootstrap.Offcanvas.getOrCreateInstance(document.getElementById('ocEditCustomer')).show();
  };

  function toast(msg) {
    if (window.Swal) {
      Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: msg, showConfirmButton: false, timer: 2200, timerProgressBar: true });
    }
  }

  function errorPopup(msg) {
    if (window.Swal) {
      Swal.fire({ icon: 'error', title: 'Không lưu được', text: msg, confirmButtonText: 'Đóng', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
    } else {
      alert(msg);
    }
  }

  document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('customerForm');
    if (!form) { return; }

    initSelect2();
    initPickers();
    initValidate();

    var typeSel = document.getElementById('oc-type');
    if (typeSel) { typeSel.addEventListener('change', syncBusiness); }

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      if ($ && !$(form).valid()) { return; }   // jQuery Validate chặn tới khi hợp lệ
      var fd = new FormData(form);
      fetch(form.getAttribute('data-action'), {
        method: 'POST',
        headers: { 'RequestVerificationToken': token() },
        body: fd
      })
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res && res.isSuccess) {
            bootstrap.Offcanvas.getInstance(document.getElementById('ocEditCustomer')).hide();
            var isDt = window.jQuery && jQuery.fn.DataTable && jQuery.fn.DataTable.isDataTable('#tbl-customers');
            if (isDt) {
              jQuery('#tbl-customers').DataTable().ajax.reload(null, false);
              toast(res.message || 'Đã lưu khách hàng.');
            } else if (typeof window.tkGridReload === 'function') {
              // Lưới không phải DataTables (vd Tabulator) tự đăng ký hàm nạp lại → khỏi tải lại cả trang.
              window.tkGridReload();
              toast(res.message || 'Đã lưu khách hàng.');
            } else {
              location.reload();
            }
          } else {
            errorPopup((res && (res.message || res.detail || res.title)) || 'Lưu thất bại.');
          }
        })
        .catch(function () { errorPopup('Lỗi kết nối, thử lại.'); });
    });
  });
})();
