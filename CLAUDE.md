<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **tourkit-crm** (11787 symbols, 77156 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> If any GitNexus tool warns the index is stale, run `npx gitnexus analyze` in terminal first.

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `gitnexus_impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `gitnexus_detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `gitnexus_query({query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `gitnexus_context({name: "symbolName"})`.

## When Debugging

1. `gitnexus_query({query: "<error or symptom>"})` — find execution flows related to the issue
2. `gitnexus_context({name: "<suspect function>"})` — see all callers, callees, and process participation
3. `READ gitnexus://repo/tourkit-crm/process/{processName}` — trace the full execution flow step by step
4. For regressions: `gitnexus_detect_changes({scope: "compare", base_ref: "main"})` — see what your branch changed

## When Refactoring

- **Renaming**: MUST use `gitnexus_rename({symbol_name: "old", new_name: "new", dry_run: true})` first. Review the preview — graph edits are safe, text_search edits need manual review. Then run with `dry_run: false`.
- **Extracting/Splitting**: MUST run `gitnexus_context({name: "target"})` to see all incoming/outgoing refs, then `gitnexus_impact({target: "target", direction: "upstream"})` to find all external callers before moving code.
- After any refactor: run `gitnexus_detect_changes({scope: "all"})` to verify only expected files changed.

## Never Do

- NEVER edit a function, class, or method without first running `gitnexus_impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `gitnexus_rename` which understands the call graph.
- NEVER commit changes without running `gitnexus_detect_changes()` to check affected scope.

## Tools Quick Reference

| Tool | When to use | Command |
|------|-------------|---------|
| `query` | Find code by concept | `gitnexus_query({query: "auth validation"})` |
| `context` | 360-degree view of one symbol | `gitnexus_context({name: "validateUser"})` |
| `impact` | Blast radius before editing | `gitnexus_impact({target: "X", direction: "upstream"})` |
| `detect_changes` | Pre-commit scope check | `gitnexus_detect_changes({scope: "staged"})` |
| `rename` | Safe multi-file rename | `gitnexus_rename({symbol_name: "old", new_name: "new", dry_run: true})` |
| `cypher` | Custom graph queries | `gitnexus_cypher({query: "MATCH ..."})` |

## Impact Risk Levels

| Depth | Meaning | Action |
|-------|---------|--------|
| d=1 | WILL BREAK — direct callers/importers | MUST update these |
| d=2 | LIKELY AFFECTED — indirect deps | Should test |
| d=3 | MAY NEED TESTING — transitive | Test if critical path |

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/tourkit-crm/context` | Codebase overview, check index freshness |
| `gitnexus://repo/tourkit-crm/clusters` | All functional areas |
| `gitnexus://repo/tourkit-crm/processes` | All execution flows |
| `gitnexus://repo/tourkit-crm/process/{name}` | Step-by-step execution trace |

## Self-Check Before Finishing

Before completing any code modification task, verify:
1. `gitnexus_impact` was run for all modified symbols
2. No HIGH/CRITICAL risk warnings were ignored
3. `gitnexus_detect_changes()` confirms changes match expected scope
4. All d=1 (WILL BREAK) dependents were updated

## Keeping the Index Fresh

After committing code changes, the GitNexus index becomes stale. Re-run analyze to update it:

```bash
npx gitnexus analyze
```

If the index previously included embeddings, preserve them by adding `--embeddings`:

```bash
npx gitnexus analyze --embeddings
```

To check whether embeddings exist, inspect `.gitnexus/meta.json` — the `stats.embeddings` field shows the count (0 means no embeddings). **Running analyze without `--embeddings` will delete any previously generated embeddings.**

> Claude Code users: A PostToolUse hook handles this automatically after `git commit` and `git merge`.

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

# EF Core / truy vấn dữ liệu

Trước khi viết hay sửa BẤT KỲ truy vấn nào, đọc `.claude/skills/ef-core/SKILL.md`.

## Luật bắt buộc của repo này

1. **Lọc / sắp xếp / phân trang / đếm / cộng phải làm ở SQL.** Cấm nạp cả bảng rồi
   `.Where()`/`.Skip()`/`.Count()`/`.Sum()` trong bộ nhớ. Nhớ rằng
   `IRepository.ListAsync(predicate)` **materialize ngay** — mọi thứ viết sau nó là LINQ-to-Objects.
   Cần đếm thì dùng `CountAsync`, cần kiểm tra tồn tại thì `AnyAsync`, cần phân trang thì `PageAsync`.

2. **Danh sách nghiệp vụ luôn phân trang từ server** (`tk.table` + `OnGetDataAsync` + `DtJson`).
   Chỉ danh mục nhỏ mới được `tk.tableClient`. Xem `no-get-all-server-paging`.

3. **Khung nhìn giàu (lịch, kanban, ma trận) nạp có biên**: kanban lấy top-N mỗi cột;
   lịch chỉ nạp khoảng đang xem; ma trận giới hạn cửa sổ ngày + phân trang hàng.

4. **Đừng tra bảng để lấy thứ đã có trong cookie.** Tên/email/quyền của người đang đăng nhập nằm
   sẵn trong claim (`name`, `email`, `perm`) — đọc `User.FindFirst(...)`, không truy vấn bảng Users.

5. **Danh mục tra cứu lặp lại thì dùng cache**, đừng gọi lại mỗi lần AJAX phân trang.
   Danh bạ nhân viên đã có `TourKit.Api.Services.UserDirectory` (cache 60 giây, tách theo tenant) —
   dùng nó thay cho `IUserAdminService.ListAsync()` ở các handler dữ liệu.

6. **Ngày nghiệp vụ neo offset 0** bằng `TkDate.Day()`. Xem mục Ngày tháng bên dưới.

## Lệch pha đã biết giữa skill và repo

Skill khuyên "đừng bọc DbContext trong Repository". Repo này **đang** bọc (`IRepository<T>`), và
609 test dựa vào `FakeRepository`. **Không** tự ý bỏ lớp repository — nếu thấy cần, hỏi trước.
Trong lúc còn lớp này: query phức tạp thì tạo interface repo riêng đẩy được xuống SQL, chứ đừng
lọc trong bộ nhớ cho tiện.

## Ngày tháng

Ngày nghiệp vụ (khởi hành, ngày sinh, hạn thanh toán, ngày hoá đơn) **không có múi giờ**.
Lưu phải qua `TkDate.Day()` (bỏ giờ, neo `TimeSpan.Zero`). Gọi `.ToUniversalTime()` trên loại
field này sẽ làm ngày **lùi một ngày** ở múi giờ VN. Mốc thời gian thật (giờ nhắc, giờ tạo) thì
ngược lại: giữ UTC.

Ô ngày trên giao diện luôn cấu hình flatpickr `dateFormat:'Y-m-d', altInput:true, altFormat:'d/m/Y'`
— gửi ISO, hiện d/m/Y. Gửi thẳng `dd/MM/yyyy` thì model binding không parse được và field âm thầm
về null (mất dữ liệu).
