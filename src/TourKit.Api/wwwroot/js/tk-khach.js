/* tk-khach.js — ô SỐ ĐIỆN THOẠI tự tra khách hàng, kèm hộp tạo nhanh.

   Khách hàng định danh bằng SĐT (luật ở CustomerService), nên mọi form có liên quan tới khách đều
   phải bắt đầu bằng câu hỏi "số này đã có ai chưa". Không hỏi thì mỗi form lại đẻ ra một khách trùng
   số, và tới lúc đối soát công nợ mới phát hiện một người có ba hồ sơ — lúc đó gộp lại rất tốn công
   vì đơn hàng đã bám vào cả ba.

   Khai bằng thuộc tính data trên chính ô số, cùng lối với data-tien-auto:
     <input name="Input.ContactPhone" data-khach-sdt
            data-khach-id="Input.CustomerId"
            data-khach-ten="Input.ContactName"
            data-khach-email="Input.ContactEmail" />

   Tách khỏi tk.js vì đây là một tính năng trọn gói (gợi ý + hộp thoại), không phải tiện ích lặt vặt. */
(function () {
  'use strict';
  var tk = window.tk || (window.tk = {});

  tk.khachSdt = function (root) {
    $(root || document).find('input[data-khach-sdt]').each(function () {
      var $sdt = $(this);
      if ($sdt.data('tkKhachDaGan')) { return; }   // gắn một lần, mở lại form không nhân đôi
      $sdt.data('tkKhachDaGan', true);

      var $form = $sdt.closest('form');
      function o(ten) { return ten ? $form.find('[name="' + ten + '"]') : $(); }
      var $id = o($sdt.data('khach-id'));
      var $ten = o($sdt.data('khach-ten'));
      var $email = o($sdt.data('khach-email'));

      var $boc = $('<div class="tk-khach"></div>');
      $sdt.after($boc);
      var $bao = $('<div class="tk-khach-bao small mt-1"></div>').appendTo($boc);
      var $goi = $('<div class="tk-khach-goi d-none"></div>').appendTo($boc);
      var doiSo = null;
      var dsGoiY = [];

      function dongGoiY() { $goi.addClass('d-none').empty(); }

      function veTrong() {
        $bao.empty().removeClass('tk-khach-co tk-khach-chua');
        dongGoiY();
      }

      function veDaCo(k) {
        $id.val(k.id);
        // Điền hộ CHỈ KHI ô đang trống: người dùng vừa gõ tay mà bị ghi đè là mất việc họ vừa làm.
        // Tên khách vẫn hiện ở dòng trạng thái nên không ai gắn nhầm người.
        if ($ten.length && !$ten.val()) { $ten.val(k.fullName); }
        if ($email.length && !$email.val() && k.email) { $email.val(k.email); }
        dongGoiY();
        $bao.removeClass('tk-khach-chua').addClass('tk-khach-co').html(
          '<i class="ti ti-user-check me-1"></i>Đã có hồ sơ: <strong>' + tk.escape(k.fullName) + '</strong>' +
          (k.code ? ' <span class="text-muted">(' + tk.escape(k.code) + ')</span>' : ''));
      }

      function veChuaCo() {
        // XOÁ id cũ: sửa số từ khách A sang một số chưa ai dùng mà vẫn giữ id của A thì bản ghi gắn
        // nhầm người — sai im lặng, không ô nào báo.
        $id.val('');
        $bao.removeClass('tk-khach-co').addClass('tk-khach-chua').html(
          '<i class="ti ti-user-question me-1"></i>Chưa có khách nào dùng số này. ' +
          '<button type="button" class="btn btn-sm btn-label-primary py-0 px-2 tk-khach-tao">Tạo nhanh</button>');
      }

      // Gợi ý thả xuống khi số đang gõ dở khớp NHIỀU khách: người dùng nhớ "khách đó đuôi 888" là
      // chọn được ngay, không phải nhớ trọn mười chữ số.
      function veGoiY(ds) {
        dsGoiY = ds;
        var html = '';
        for (var i = 0; i < ds.length; i++) {
          var k = ds[i];
          html += '<button type="button" class="tk-khach-mot" data-i="' + i + '">' +
            '<span class="fw-medium">' + tk.escape(k.fullName) + '</span>' +
            '<span class="text-muted ms-2">' + tk.escape(k.phone || '') + '</span>' +
            (k.code ? '<span class="text-muted ms-2">' + tk.escape(k.code) + '</span>' : '') +
            '</button>';
        }
        $goi.removeClass('d-none').html(html);
        $bao.empty().removeClass('tk-khach-co tk-khach-chua');
      }

      function tra() {
        var sdt = ($sdt.val() || '').trim();
        if (sdt.replace(/\D/g, '').length < 3) { veTrong(); $id.val(''); return; }

        $.getJSON('?handler=KhachTheoSdt&sdt=' + encodeURIComponent(sdt), function (r) {
          if (!r) { return; }
          if (r.khop) { veDaCo(r.khop); return; }                       // khớp đúng số → gắn luôn
          if (r.results && r.results.length) { veGoiY(r.results); return; }
          veChuaCo();
        });
      }

      // Chờ ngưng gõ rồi mới hỏi: bắn mỗi phím một request thì một số điện thoại sinh ra 10 lượt tra.
      $sdt.on('input', function () { clearTimeout(doiSo); doiSo = setTimeout(tra, 350); });
      // Rời ô thì tra nốt, nhưng chờ một nhịp để cú bấm vào dòng gợi ý kịp chạy trước.
      $sdt.on('blur', function () { clearTimeout(doiSo); setTimeout(tra, 200); });

      $goi.on('mousedown', '.tk-khach-mot', function (e) {
        // mousedown chứ không click: blur của ô số chạy trước click và có thể đóng mất danh sách.
        e.preventDefault();
        var k = dsGoiY[Number($(this).attr('data-i'))];
        if (!k) { return; }
        if (k.phone) { $sdt.val(k.phone); }
        veDaCo(k);
      });

      $bao.on('click', '.tk-khach-tao', function () {
        tk.khachTaoNhanh({
          ten: ($ten.val() || '').trim(),
          sdt: ($sdt.val() || '').trim(),
          email: ($email.val() || '').trim(),
          xong: function (k) { veDaCo(k); }
        });
      });

      // Mở form sửa một bản ghi đã gắn khách: hiện ngay trạng thái, đừng đợi người dùng chạm vào ô.
      if (($sdt.val() || '').trim()) { tra(); }
    });
  };

  /**
   * Hộp TẠO NHANH khách. Dựng MỘT lần rồi dùng lại, không nhồi thêm DOM mỗi lần bấm.
   *
   * Vì sao là hộp riêng chứ không tạo thẳng từ ô đang gõ: tạo hồ sơ khách là việc có hệ quả lâu dài
   * (mọi đơn hàng sau này bám vào nó), nên người dùng cần nhìn thấy đủ thứ sắp lưu và sửa được trước
   * khi bấm. Tạo lén sau lưng bằng một cú bấm là cách nhanh nhất để có một danh bạ đầy hồ sơ sai tên.
   */
  tk.khachTaoNhanh = function (opts) {
    opts = opts || {};
    var el = document.getElementById('tk-khach-modal');
    if (!el) {
      el = document.createElement('div');
      el.id = 'tk-khach-modal';
      el.className = 'modal fade';
      el.tabIndex = -1;
      el.innerHTML =
        '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
          '<div class="modal-header"><h5 class="modal-title">Tạo nhanh khách hàng</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>' +
          '<div class="modal-body">' +
            '<p class="text-muted small">Khách định danh bằng số điện thoại — số này chưa có ai dùng. ' +
              'Phần hồ sơ đầy đủ bổ sung sau ở màn Khách hàng.</p>' +
            '<div class="mb-3"><label class="form-label">Tên khách <span class="text-danger">*</span></label>' +
              '<input type="text" class="form-control" id="tk-khach-ten" placeholder="Nguyễn Thị Lan" /></div>' +
            '<div class="mb-3"><label class="form-label">Điện thoại <span class="text-danger">*</span></label>' +
              '<input type="text" class="form-control" id="tk-khach-sdt" placeholder="09xxxxxxxx" /></div>' +
            '<div class="mb-1"><label class="form-label">Email</label>' +
              '<input type="text" class="form-control" id="tk-khach-email" placeholder="ten@congty.vn" /></div>' +
          '</div>' +
          '<div class="modal-footer">' +
            '<button type="button" class="btn btn-label-secondary" data-bs-dismiss="modal">Đóng</button>' +
            '<button type="button" class="btn btn-primary" id="tk-khach-luu">Tạo khách</button>' +
          '</div>' +
        '</div></div>';
      document.body.appendChild(el);
    }

    $('#tk-khach-ten').val(opts.ten || '');
    $('#tk-khach-sdt').val(opts.sdt || '');
    $('#tk-khach-email').val(opts.email || '');

    var hop = bootstrap.Modal.getOrCreateInstance(el);

    // Gắn LẠI mỗi lần mở: hàm gọi lại (xong) khác nhau theo từng form đang dùng hộp này.
    $('#tk-khach-luu').off('click').on('click', function () {
      var ten = ($('#tk-khach-ten').val() || '').trim();
      if (!ten) { tk.error('Bắt buộc nhập tên khách.'); $('#tk-khach-ten').trigger('focus'); return; }

      var fd = new FormData();
      fd.append('fullName', ten);
      fd.append('phone', ($('#tk-khach-sdt').val() || '').trim());
      fd.append('email', ($('#tk-khach-email').val() || '').trim());
      tk.post('?handler=TaoNhanhKhach', fd).then(function (res) {
        if (res && res.isSuccess) {
          hop.hide();
          tk.toast(res.message);
          if (opts.xong) { opts.xong(res.data); }
        } else {
          // Luật chặn trùng SĐT nằm ở tầng dịch vụ nên bắt được cả khi hai người cùng bấm một lúc.
          tk.error((res && res.message) || 'Không tạo được khách.');
        }
      });
    });

    hop.show();
    setTimeout(function () { $('#tk-khach-ten').trigger('focus'); }, 300);
  };
})();
