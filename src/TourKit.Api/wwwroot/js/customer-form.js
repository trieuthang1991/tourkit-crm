/* Offcanvas thêm/sửa khách hàng: Select2 (search + tags multi) + section Doanh nghiệp động + lưu AJAX. */
(function () {
  'use strict';

  var $ = window.jQuery;

  function token() {
    var el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
  }

  // Set giá trị: select2 cần set qua jQuery + trigger; input thường dùng .value.
  function setVal(name, v) {
    var el = document.querySelector('#customerForm [name="' + name + '"]');
    if (!el) { return; }
    if ($ && (el.classList.contains('oc-select2') || el.classList.contains('oc-select2-tags'))) {
      $(el).val(v === null || v === undefined || v === '' ? null : v).trigger('change');
    } else {
      el.value = (v === null || v === undefined) ? '' : v;
    }
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

  function syncBusiness() {
    var sel = document.getElementById('oc-type');
    if (!sel) { return; }
    var opt = sel.options[sel.selectedIndex];
    var isOrg = opt && opt.getAttribute('data-business') === 'true';
    var box = document.getElementById('oc-business');
    var unit = document.getElementById('oc-unitname');
    if (box) { box.classList.toggle('d-none', !isOrg); }
    if (unit) { unit.required = !!isOrg; }
  }

  // data = null -> thêm mới; data = {...} -> sửa.
  window.openCustomerOffcanvas = function (data) {
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
    setVal('Input.Tags', data && data.tags ? data.tags : []);   // multi
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
    var host = document.getElementById('js-toast');
    if (!host) { return; }
    var el = document.createElement('div');
    el.className = 'bs-toast toast show bg-success text-white mb-2';
    el.setAttribute('role', 'alert');
    el.innerHTML = '<div class="toast-body d-flex align-items-center"><i class="ti ti-check me-2"></i>' + msg + '</div>';
    host.appendChild(el);
    setTimeout(function () { el.remove(); }, 2500);
  }

  document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('customerForm');
    if (!form) { return; }

    initSelect2();

    var typeSel = document.getElementById('oc-type');
    if (typeSel) { typeSel.addEventListener('change', syncBusiness); }

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      var fd = new FormData(form);
      fetch(form.getAttribute('data-action'), {
        method: 'POST',
        headers: { 'RequestVerificationToken': token() },
        body: fd
      })
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res && res.ok) {
            bootstrap.Offcanvas.getInstance(document.getElementById('ocEditCustomer')).hide();
            var isDt = window.jQuery && jQuery.fn.DataTable && jQuery.fn.DataTable.isDataTable('#tbl-customers');
            if (isDt) {
              jQuery('#tbl-customers').DataTable().ajax.reload(null, false);
              toast('Đã lưu khách hàng.');
            } else {
              location.reload();
            }
          } else {
            alert((res && res.error) || 'Lưu thất bại.');
          }
        })
        .catch(function () { alert('Lỗi kết nối, thử lại.'); });
    });
  });
})();
