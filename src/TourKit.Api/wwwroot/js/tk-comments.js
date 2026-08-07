/* tk-comments.js — luồng bình luận gắn được vào MỌI màn chi tiết.
   Dùng: <div data-comments data-entity="Customer" data-entity-id="@c.Id"></div>
   Tự tìm mọi phần tử [data-comments] lúc trang tải xong, không cần gọi tay ở từng màn. */
(function () {
  'use strict';
  var tk = window.tk;
  if (!tk) { return; }

  var API = '/binh-luan';

  // Thời gian tương đối dùng lại tk.ago() của chuông thông báo — cùng cách đọc trên toàn hệ thống.
  function when(iso) { return tk.ago ? tk.ago(iso) : new Date(iso).toLocaleString('vi-VN'); }

  function initials(name) {
    var parts = (name || '?').trim().split(/\s+/);
    return (parts[parts.length - 1][0] || '?').toUpperCase();
  }

  // Bỏ dấu để gõ "cuong" cũng ra "Lê Cường" — người Việt gõ tên đồng nghiệp thường không bỏ dấu.
  function plain(s) {
    return (s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
  }

  // Danh bạ nạp MỘT LẦN cho mỗi LOẠI bản ghi (không phải cho cả trang): danh sách người được nhắc
  // khác nhau theo loại, vì chỉ gợi ý người có quyền xem bản ghi đó.
  var peopleCache = {};
  function people(entity) {
    if (!peopleCache[entity]) {
      peopleCache[entity] = fetch(API + '?handler=People&entityName=' + encodeURIComponent(entity),
        { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function (r) { return r.ok ? r.json() : []; })
        .catch(function () { return []; });
    }
    return peopleCache[entity];
  }

  function escapeRe(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }

  // Tô đậm CHỈ những cái tên server xác nhận là người thật. Tự dò chữ "@..." trong nội dung sẽ
  // tô nhầm cả "@giá tốt" thành một cái tên.
  function withMentions(escaped, names) {
    if (!names || !names.length) { return escaped; }
    // Tên dài trước để "Lê Cường" không bị "Lê" ăn mất phần sau.
    var sorted = names.slice().sort(function (a, b) { return b.length - a.length; });
    sorted.forEach(function (n) {
      var re = new RegExp('@' + escapeRe(tk.escape(n)), 'g');
      escaped = escaped.replace(re, '<span class="tk-cmt-at">@' + tk.escape(n) + '</span>');
    });
    return escaped;
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
        // Nội dung do người dùng gõ — LUÔN escape rồi mới tô @nhắc, không bao giờ đổ thẳng HTML.
        (c.content
          ? '<div class="tk-cmt-text">' +
              withMentions(tk.escape(c.content), c.mentions).replace(/\n/g, '<br>') +
            '</div>'
          : '') +
        thumbs(c.attachments) +
      '</div></li>';
  }

  // Ảnh đã gửi: bấm mở tab mới xem cỡ thật. loading="lazy" để luồng dài không tải hết ảnh một lúc.
  function thumbs(list) {
    if (!list || !list.length) { return ''; }
    return '<div class="tk-cmt-imgs">' + list.map(function (a) {
      return '<a href="' + tk.escape(a.url) + '" target="_blank" rel="noopener" title="' + tk.escape(a.name) + '">' +
        '<img src="' + tk.escape(a.url) + '" alt="' + tk.escape(a.name) + '" loading="lazy">' +
        '</a>';
    }).join('') + '</div>';
  }

  /// Ô gợi ý nhân viên khi gõ "@". Trả về đối tượng có picked() để lấy danh sách id đã chọn.
  function mentionPicker(input, menu, entity) {
    var picked = {};        // tên → id, giữ để lúc gửi biết ai được nhắc
    var matches = [];
    var active = -1;

    function close() { menu.classList.add('d-none'); matches = []; active = -1; }

    // Token "@..." đang gõ dở, tính từ dấu @ gần nhất trước con trỏ.
    function token() {
      var pos = input.selectionStart;
      var upto = input.value.slice(0, pos);
      var at = upto.lastIndexOf('@');
      if (at < 0) { return null; }
      // Phải đứng đầu dòng hoặc sau khoảng trắng — "email@congty.vn" không được kích hoạt ô chọn.
      if (at > 0 && !/\s/.test(upto[at - 1])) { return null; }
      var q = upto.slice(at + 1);
      if (/[\n]/.test(q)) { return null; }
      return { at: at, q: q, end: pos };
    }

    function render() {
      menu.innerHTML = matches.map(function (p, i) {
        return '<button type="button" class="tk-cmt-at-item' + (i === active ? ' active' : '') + '" data-i="' + i + '">' +
          '<span class="tk-cmt-at-name">' + tk.escape(p.name) + '</span>' +
          (p.dept ? '<span class="tk-cmt-at-dept">' + tk.escape(p.dept) + '</span>' : '') +
          '</button>';
      }).join('');
      menu.classList.toggle('d-none', matches.length === 0);
    }

    function choose(i) {
      var p = matches[i];
      var t = token();
      if (!p || !t) { return; }
      var before = input.value.slice(0, t.at);
      var after = input.value.slice(t.end);
      input.value = before + '@' + p.name + ' ' + after;
      var caret = (before + '@' + p.name + ' ').length;
      input.setSelectionRange(caret, caret);
      picked[p.name] = p.id;
      close();
      input.focus();
    }

    input.addEventListener('input', function () {
      var t = token();
      if (!t) { close(); return; }
      people(entity).then(function (all) {
        var q = plain(t.q);
        matches = all.filter(function (p) { return plain(p.name).includes(q); }).slice(0, 8);
        active = matches.length ? 0 : -1;
        render();
      });
    });

    input.addEventListener('keydown', function (e) {
      if (menu.classList.contains('d-none')) { return; }
      if (e.key === 'ArrowDown') { e.preventDefault(); active = (active + 1) % matches.length; render(); }
      else if (e.key === 'ArrowUp') { e.preventDefault(); active = (active - 1 + matches.length) % matches.length; render(); }
      else if (e.key === 'Enter' || e.key === 'Tab') { e.preventDefault(); choose(active); }
      else if (e.key === 'Escape') { close(); }
    });

    menu.addEventListener('mousedown', function (e) {
      var btn = e.target.closest('.tk-cmt-at-item');
      if (btn) { e.preventDefault(); choose(Number(btn.dataset.i)); }
    });

    input.addEventListener('blur', function () { setTimeout(close, 120); });

    return {
      close: close,
      reset: function () { picked = {}; },
      // Chỉ trả id của những cái tên CÒN trong nội dung: người dùng chọn xong rồi xoá chữ đi thì
      // không được lặng lẽ bắn thông báo cho người ta.
      picked: function (content) {
        return Object.keys(picked)
          .filter(function (name) { return content.includes('@' + name); })
          .map(function (name) { return picked[name]; });
      }
    };
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
          // KHÔNG dùng <form>: component này còn được gắn vào trong offcanvas sửa của màn khác,
          // mà form lồng form là HTML không hợp lệ và nút Gửi sẽ kích hoạt luôn form cha.
          '<div class="tk-cmt-form mb-3">' +
            '<div class="tk-cmt-input">' +
              '<textarea class="form-control" rows="2" maxlength="4000" ' +
                'placeholder="Ghi lại trao đổi với khách, lý do chưa chốt, việc cần theo... Gõ @ để nhắc đồng nghiệp."></textarea>' +
              '<div class="tk-cmt-at-menu d-none" data-role="at"></div>' +
            '</div>' +
            '<div class="tk-cmt-drafts" data-role="drafts"></div>' +
            '<div class="d-flex align-items-center justify-content-between mt-2 gap-2">' +
              '<div class="d-flex align-items-center gap-2">' +
                '<button type="button" class="btn btn-sm btn-label-secondary" data-role="pick-img" title="Đính kèm ảnh">' +
                  '<i class="ti ti-photo"></i></button>' +
                '<input type="file" accept="image/*" multiple class="d-none" data-role="file">' +
                '<span class="text-muted small d-none d-sm-inline">Gõ <kbd>@</kbd> để nhắc, hoặc dán ảnh thẳng vào ô.</span>' +
              '</div>' +
              '<button type="button" class="btn btn-sm btn-primary" data-role="send">Gửi</button>' +
            '</div>' +
          '</div>' +
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
    var picker = mentionPicker(input, root.querySelector('[data-role="at"]'), entity);

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
      fetch(API + q, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
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

    var sendBtn = form.querySelector('[data-role="send"]');
    var fileInput = form.querySelector('[data-role="file"]');
    var draftsEl = form.querySelector('[data-role="drafts"]');
    var drafts = [];   // ảnh đã tải lên nhưng CHƯA gửi kèm bình luận nào

    function renderDrafts() {
      draftsEl.innerHTML = drafts.map(function (d, i) {
        return '<span class="tk-cmt-draft">' +
          (d.loading
            ? '<span class="spinner-border spinner-border-sm"></span>'
            : '<img src="' + tk.escape(d.preview) + '" alt="">') +
          '<span class="tk-cmt-draft-name">' + tk.escape(d.name) + '</span>' +
          (d.loading ? '' : '<button type="button" class="tk-cmt-draft-x" data-i="' + i + '">&times;</button>') +
          '</span>';
      }).join('');
    }

    function upload(file) {
      if (drafts.length >= 5) { tk.error('Tối đa 5 ảnh mỗi bình luận.'); return; }

      // Xem trước bằng blob local NGAY, không chờ server: người dùng thấy ảnh mình vừa chọn tức thì.
      var draft = { name: file.name || 'ảnh', loading: true, preview: URL.createObjectURL(file) };
      drafts.push(draft);
      renderDrafts();

      var fd = new FormData();
      fd.append('entityName', entity);
      fd.append('entityId', entityId);
      fd.append('file', file);
      tk.post(API + '?handler=Upload', fd).then(function (r) {
        if (r && r.isSuccess && r.data) {
          draft.id = r.data.id;
          draft.loading = false;
        } else {
          drafts.splice(drafts.indexOf(draft), 1);
          tk.error((r && r.message) || 'Không tải được ảnh.');
        }
        renderDrafts();
      });
    }

    form.querySelector('[data-role="pick-img"]').addEventListener('click', function () { fileInput.click(); });
    fileInput.addEventListener('change', function () {
      Array.prototype.forEach.call(fileInput.files, upload);
      fileInput.value = '';   // chọn lại cùng một tệp vẫn phải kích hoạt change
    });

    draftsEl.addEventListener('click', function (e) {
      var x = e.target.closest('.tk-cmt-draft-x');
      if (!x) { return; }
      drafts.splice(Number(x.dataset.i), 1);
      renderDrafts();
    });

    // Dán ảnh thẳng từ clipboard — cách nhanh nhất để đưa một ảnh chụp màn hình vào.
    input.addEventListener('paste', function (e) {
      var items = (e.clipboardData || {}).items || [];
      Array.prototype.forEach.call(items, function (it) {
        if (it.kind === 'file' && it.type.indexOf('image/') === 0) {
          e.preventDefault();
          upload(it.getAsFile());
        }
      });
    });

    function send() {
      var content = (input.value || '').trim();
      var ready = drafts.filter(function (d) { return d.id; });

      // Ảnh không kèm chữ vẫn gửi được; nhưng đang còn ảnh tải dở thì chờ, đừng gửi thiếu.
      if (!content && ready.length === 0) { return; }
      if (drafts.some(function (d) { return d.loading; })) {
        tk.error('Đang tải ảnh, chờ một chút.');
        return;
      }

      var btn = sendBtn;
      btn.disabled = true;

      var fd = new FormData();
      fd.append('entityName', entity);
      fd.append('entityId', entityId);
      fd.append('content', content);
      fd.append('mentions', picker.picked(content).join(','));
      fd.append('attachments', ready.map(function (d) { return d.id; }).join(','));

      tk.post(API, fd).then(function (r) {
        if (r && r.isSuccess) {
          input.value = '';
          picker.reset();
          drafts.forEach(function (d) { URL.revokeObjectURL(d.preview); });
          drafts = [];
          renderDrafts();
          load();
        } else {
          tk.error((r && r.message) || 'Không gửi được, thử lại.');
        }
      }).finally(function () { btn.disabled = false; });
    }

    sendBtn.addEventListener('click', send);

    // Ctrl/Cmd+Enter gửi — người dùng gõ nhiều dòng nên Enter phải là xuống dòng.
    // Đăng ký SAU picker: khi ô gợi ý đang mở, Enter là "chọn người" chứ không phải "gửi".
    input.addEventListener('keydown', function (e) {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') { picker.close(); send(); }
    });

    list.addEventListener('click', function (e) {
      var del = e.target.closest('.tk-cmt-del');
      if (!del) { return; }
      var id = del.closest('.tk-cmt').dataset.id;

      tk.confirmDelete({ title: 'Xoá trao đổi?', text: 'Nội dung này sẽ mất hẳn.' }).then(function (ok) {
        if (!ok) { return; }
        var fd = new FormData();
        fd.append('id', id);
        tk.post(API + '?handler=Delete', fd).then(function (r) {
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
