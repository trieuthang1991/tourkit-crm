# claude-memory-compiler — tích hợp vào TourKit

Nguồn: https://github.com/coleam00/claude-memory-compiler — tự động bắt transcript phiên Claude Code
(qua hooks), trích insight bằng Claude Agent SDK, dựng knowledge base để inject lại ở phiên sau.

## Cách tích hợp (đã làm)
- Đặt gọn trong `.claude/memory/` (không làm bẩn cây source .NET/React).
- Hooks bật trong `.claude/settings.json` — chạy `uv run --directory .claude/memory python hooks/*.py`.
- Timezone đã đổi sang `Asia/Ho_Chi_Minh` (`.claude/memory/scripts/config.py`).
- Đã `uv sync` (deps + claude-agent-sdk trong `.claude/memory/.venv`).
- Nội dung sinh ra (`daily/`, `knowledge/`, `reports/`, `.venv/`, state) bị `.claude/memory/.gitignore` bỏ qua — KHÔNG commit transcript.

## ⚠️ Lưu ý vận hành
- Hooks chạy **tự động mỗi phiên** (SessionStart/PreCompact/SessionEnd) và gọi **Claude Agent SDK** →
  dùng **subscription/token của bạn** + gửi transcript tới Anthropic. Compile tự động sau ~18h giờ VN.
- Máy khác clone repo phải chạy `uv sync` trong `.claude/memory` trước, nếu không hook sẽ lỗi.

## Dùng thủ công
```bash
cd .claude/memory
uv run python scripts/compile.py          # nén daily log → bài viết concept
uv run python scripts/query.py "câu hỏi"  # tìm trong knowledge base
uv run python scripts/lint.py             # kiểm tra sức khoẻ bài viết
```

## Tắt (nếu không muốn hook tự chạy)
Xoá block `"hooks"` trong `.claude/settings.json` (hoặc xoá cả file). Tool vẫn dùng thủ công được.
