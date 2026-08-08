import { test, expect } from '../fixtures.js';

/** Khung trợ lý tra cứu — panel trượt phải, có ở mọi trang. */
test.describe('Trợ lý tra cứu', () => {
  test('Nút mở chỉ hiện khi máy chủ báo tính năng đang bật', async ({ trang }) => {
    await trang.goto('/tong-quan');

    const tt = await trang.evaluate(async () => (await fetch('/tro-ly?handler=Status')).json());
    const nut = trang.locator('#tk-ai-launch');

    if (tt.available) {
      await expect(nut).not.toHaveClass(/d-none/, { timeout: 15_000 });
    } else {
      // Tắt mà vẫn bày nút ra thì người dùng bấm vào chỉ nhận về lỗi.
      await expect(nut).toHaveClass(/d-none/);
    }
  });

  test('Ctrl+K mở panel, có gợi ý câu hỏi', async ({ trang }) => {
    await trang.goto('/tong-quan');
    await trang.locator('#tk-ai-launch:not(.d-none)').waitFor({ timeout: 20_000 });

    await trang.keyboard.press('Control+K');
    await expect(trang.locator('#tk-ai-panel')).toHaveClass(/show/, { timeout: 10_000 });
    expect(await trang.locator('.tk-ai-goiy').count()).toBeGreaterThan(0);
  });

  /**
   * Trợ lý phải trả về BẢNG số liệu kèm link sang màn hình gốc, không phải một đoạn văn kể số.
   * Có bảng thì người dùng kiểm chứng được ngay; chỉ có văn xuôi thì phải tin lời model.
   */
  test('@ai Hỏi một câu và nhận lại bảng số liệu kèm link', async ({ trang }) => {
    test.slow();

    await trang.goto('/tong-quan');
    await trang.locator('#tk-ai-launch:not(.d-none)').waitFor({ timeout: 20_000 });
    await trang.keyboard.press('Control+K');
    await expect(trang.locator('#tk-ai-panel')).toHaveClass(/show/);

    await trang.fill('#tk-ai-panel .tk-ai-input', 'Dòng tiền theo phương thức thanh toán thế nào?');
    await trang.locator('#tk-ai-panel .tk-ai-send').click();

    // Ba chấm biến mất = đã trả lời xong.
    await expect(trang.locator('#tk-ai-panel .tk-ai-dots')).toHaveCount(0, { timeout: 100_000 });

    const traLoi = trang.locator('#tk-ai-panel .tk-ai-bot').last();
    await expect(traLoi).toBeVisible();
    await expect(traLoi.locator('table')).toBeVisible();
    await expect(traLoi.locator('a:has-text("Mở màn hình đầy đủ")')).toHaveAttribute('href', /\S+/);

    const chu = await traLoi.innerText();
    expect(chu).not.toContain('**');
    expect(chu.toLowerCase()).not.toContain('lỗi kết nối');
  });

  /**
   * Trả lời phải bám số liệu thật. So một con số trợ lý nêu với chính con số trên màn Tổng quan —
   * lệch nhau nghĩa là model đang bịa hoặc đọc nhầm nguồn.
   */
  test('@ai Số liệu trợ lý nêu khớp với màn Tổng quan', async ({ trang }) => {
    test.slow();

    await trang.goto('/tong-quan');
    const soTrenMan = (await trang.locator('body').innerText()).match(/Doanh thu\s+([\d.,]+\s*(tỷ|tr))/i)?.[1];
    expect(soTrenMan, 'không đọc được doanh thu trên màn Tổng quan').toBeTruthy();

    await trang.locator('#tk-ai-launch:not(.d-none)').waitFor({ timeout: 20_000 });
    await trang.keyboard.press('Control+K');
    await trang.fill('#tk-ai-panel .tk-ai-input', 'Tổng quan công ty: doanh thu bao nhiêu?');
    await trang.locator('#tk-ai-panel .tk-ai-send').click();
    await expect(trang.locator('#tk-ai-panel .tk-ai-dots')).toHaveCount(0, { timeout: 100_000 });

    // Bảng trong panel rút gọn "94,3 tỷ"; so phần số nguyên là đủ để phát hiện bịa.
    const nguyen = soTrenMan.match(/^\d+/)?.[0];
    await expect(trang.locator('#tk-ai-panel .tk-ai-log')).toContainText(nguyen, { timeout: 10_000 });
  });
});
