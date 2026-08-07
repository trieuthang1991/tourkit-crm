/* tk-comments.js — luồng bình luận gắn được vào MỌI màn chi tiết.
   Dùng: <div data-comments data-entity="Customer" data-entity-id="@c.Id"></div>
   Tự tìm mọi phần tử [data-comments] lúc trang tải xong, không cần gọi tay ở từng màn. */
(function () {
  'use strict';
  var tk = window.tk;
  if (!tk) { return; }

  var URL = '/binh-luan';

  // Thời gian tương đối dùng lại tk.ago() của chuông thông báo — cùng cách đọc trên toàn hệ thống.
  function when(iso) { return tk.ago ? tk.ago(iso) : new Date(iso).toLocaleString('vi-VN'); }

  function initials(name) {
    var parts = (name || '?').trim().split(/\s+/);
    return (parts[parts.length - 1][0] || '?').toUpperCase();
  }

  function row(c) {
    return '<li class="tk-cmt" data-id="' + c.id + '">' +
      '<div class="tk-cmt-ava">' + tk.escape(initials(c.author)) + '</div>' +
      '<div class="tk-cmt-body">' +
        '<div class="tk-cmt-head">' +
          '<span class="tk-cmt-author">' + tk.escape(c.author) + '</span>' +
          '<span class="tk-cmt-time">' + tk.escape(when(c.createdAt)) + '</span>' +
          (c.canDelete
            ? '<button type="button" class="btn btn-icon btn-sm tk-cmt-del" title="Xoá"><i class="ti ti-trash"></i></button>'
            : '') +
        '</div>' +
        // Nội dung do người dùng gõ — LUÔN escape rồi mới xuống dòng, không bao giờ đổ thẳng HTML.
        '<div class="tk-cmt-text">' + tk.escape(c.content).replace(/\n/g, '<br>') + '</div>' +
      '</div></li>';
  }

  function mount(root) {
    var entity = root.dataset.entity;
    var entityId = root.dataset.entityId;
    if (!entity || !entityId) { return; }

    root.innerHTML =
      '<div class="card tk-cmt-card">' +
        '<h5 class="card-header d-flex align-items-center">' +
          '<i class="ti ti-message-circle me-2"></i>Trao đổi' +
          '<span class="badge bg-label-secondary ms-2" data-role="count">0</span>' +
        '</h5>' +
        '<div class="card-body">' +
          '<form class="tk-cmt-form mb-3">' +
            '<textarea class="form-control" rows="2" maxlength="4000" ' +
              'placeholder="Ghi lại trao đổi với khách, lý do chưa chốt, việc cần theo..."></textarea>' +
            '<div class="d-flex justify-content-end mt-2">' +
              '<button type="submit" class="btn btn-sm btn-primary">Gửi</button>' +
            '</div>' +
          '</form>' +
          '<ul class="tk-cmt-list list-unstyled mb-0" data-role="list">' +
            '<li class="text-muted small">Đang tải...</li>' +
          '</ul>' +
          '<div class="text-muted small mt-2 d-none" data-role="more"></div>' +
        '</div>' +
      '</div>';

    var list = root.querySelector('[data-role="list"]');
    var count = root.querySelector('[data-role="count"]');
    var more = root.querySelector('[data-role="more"]');
    var form = root.querySelector('.tk-cmt-form');
    var input = form.querySelector('textarea');

    function render(data) {
      count.textContent = data.total;
      list.innerHTML = data.items.length
        ? data.items.map(row).join('')
        : '<li class="text-muted small">Chưa có trao đổi nào. Ghi dòng đầu tiên đi.</li>';

      // Luồng luôn nạp có biên — nói rõ còn bao nhiêu bị cắt thay vì im lặng giấu đi.
      var hidden = data.total - data.items.length;
      more.classList.toggle('d-none', hidden <= 0);
      more.textContent = hidden > 0 ? 'Còn ' + hidden + ' trao đổi cũ hơn không hiển thị.' : '';
    }

    function load() {
      var q = '?entityName=' + encodeURIComponent(entity) + '&entityId=' + encodeURIComponent(entityId);
      fetch(URL + q, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function (r) { return r.ok ? r.json() : null; })
        .then(function (d) {
          if (!d) {
            list.innerHTML = '<li class="text-muted small">Không xem được trao đổi của mục này.</li>';
            return;
          }
          render(d);
        })
        .catch(function () {
          list.innerHTML = '<li class="text-muted small">Lỗi kết nối, thử tải lại trang.</li>';
        });
    }

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      var content = (input.value || '').trim();
      if (!content) { return; }

      var btn = form.querySelector('button[type="submit"]');
      btn.disabled = true;

      var fd = new FormData();
      fd.append('entityName', entity);
      fd.append('entityId', entityId);
      fd.append('content', content);

      tk.post(URL, fd).then(function (r) {
        if (r && r.isSuccess) {
          input.value = '';
          load();
        } else {
          tk.error((r && r.message) || 'Không gửi được, thử lại.');
        }
      }).finally(function () { btn.disabled = false; });
    });

    // Ctrl/Cmd+Enter gửi — người dùng gõ nhiều dòng nên Enter phải là xuống dòng.
    input.addEventListener('keydown', function (e) {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') { form.requestSubmit(); }
    });

    list.addEventListener('click', function (e) {
      var del = e.target.closest('.tk-cmt-del');
      if (!del) { return; }
      var id = del.closest('.tk-cmt').dataset.id;

      tk.confirmDelete({ title: 'Xoá trao đổi?', text: 'Nội dung này sẽ mất hẳn.' }).then(function (ok) {
        if (!ok) { return; }
        var fd = new FormData();
        fd.append('id', id);
        tk.post(URL + '?handler=Delete', fd).then(function (r) {
          if (r && r.isSuccess) { load(); } else { tk.error((r && r.message) || 'Không xoá được.'); }
        });
      });
    });

    load();
  }

  tk.comments = { mount: mount };

  $(function () {
    document.querySelectorAll('[data-comments]').forEach(mount);
  });
})();
