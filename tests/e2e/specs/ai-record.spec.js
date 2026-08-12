import { test, expect, idKhachHangBatKy, themTraoDoi } from '../fixtures.js';

/**
 * Ba việc AI làm trên một bản ghi: chấm điểm, tóm tắt, soạn tin.
 *
 * Gắn thẻ @ai vì chúng gọi model THẬT — tốn tiền và mất tới 20 giây một lượt. Chạy riêng bằng
 * `npm run test:ai`, không nằm trong lượt chạy mặc định.
 */
test.describe('@ai Trợ lý AI trên bản ghi', () => {
  // Một lượt chấm điểm bằng model suy luận mất 15-20 giây; ba nút cộng lại vượt xa mức mặc định.
  test.slow();

  /** Mở một khách hàng và bảo đảm hồ sơ CÓ dữ liệu để AI đọc. */
  async function moKhachCoTraoDoi(trang) {
    const id = await idKhachHangBatKy(trang);
    await trang.goto(`/khach-hang/${id}`);
    await trang.locator('[data-ai-review] [data-act]').first().waitFor({ timeout: 30_000 });

    const soDong = await trang.locator('[data-comments] .tk-cmt-card, [data-comments] li').count();
    if (soDong === 0) {
      await themTraoDoi(trang, 'Khách hỏi tour Đà Nẵng cho gia đình 6 người, dự kiến giữa tháng 10.');
      await themTraoDoi(trang, 'Đã gửi báo giá 34 triệu, khách nói để bàn lại rồi trả lời.');
    }
    return id;
  }

  async function bam(trang, act) {
    await trang.locator(`[data-ai-review] [data-act="${act}"]`).click();

    // Chờ CẢ kết quả lẫn lỗi. Chỉ chờ kết quả thì khi hỏng, bài test treo tới hết giờ rồi báo
    // "timeout" — che mất thông báo lỗi thật đang hiện trên màn hình.
    const ket = trang.locator('[data-ai-review] .tk-ai-text, [data-ai-review] .tk-ai-score');
    const loi = trang.locator('[data-ai-review] .text-danger');
    await expect(ket.or(loi).first()).toBeVisible({ timeout: 100_000 });

    if (await loi.count()) {
      throw new Error(`Nút "${act}" báo lỗi: ${await loi.first().innerText()}`);
    }
  }

  test('Thẻ AI có đủ ba nút', async ({ trang }) => {
    await moKhachCoTraoDoi(trang);

    const nut = await trang.locator('[data-ai-review] [data-act]').allTextContents();
    expect(nut.map((s) => s.trim())).toEqual(['Chấm điểm', 'Tóm tắt', 'Soạn tin']);
  });

  test('Tóm tắt trả về nội dung tiếng Việt', async ({ trang }) => {
    await moKhachCoTraoDoi(trang);
    await bam(trang, 'Summary');

    const text = await trang.locator('[data-ai-review] .tk-ai-text').innerText();
    expect(text.length, 'tóm tắt quá ngắn để có ích').toBeGreaterThan(80);
    // Model được dặn không dùng markdown — sót lại thì người đọc thấy dấu sao trần.
    expect(text).not.toContain('**');
  });

  test('Soạn tin có nút chép và không bịa cam kết', async ({ trang }) => {
    await moKhachCoTraoDoi(trang);
    await bam(trang, 'Draft');

    await expect(trang.locator('[data-ai-review] [data-role="copy"]')).toBeVisible();

    const tin = await trang.locator('[data-ai-review] .tk-ai-text').innerText();
    expect(tin.length).toBeGreaterThan(40);
    // Tin nhắn này gửi thẳng cho khách; một cam kết bịa ra thì không rút lại được.
    expect(tin.toLowerCase()).not.toMatch(/giảm giá|khuyến mãi|cam kết|chắc chắn còn chỗ|đảm bảo/);
  });

  /**
   * Điểm tổng do HỆ THỐNG tính theo trọng số, không phải model tự phán một con số.
   * Bài này chứng minh điều đó ngay trên màn hình: cộng lại từ bảng chi tiết phải ra đúng số lớn.
   */
  test('Chấm điểm: tổng đúng bằng trung bình có trọng số của các tiêu chí', async ({ trang }) => {
    await moKhachCoTraoDoi(trang);
    await bam(trang, 'Review');

    const diem = Number((await trang.locator('[data-ai-review] .tk-ai-score').innerText()).trim());
    expect(Number.isFinite(diem)).toBe(true);
    expect(diem).toBeGreaterThanOrEqual(0);
    expect(diem).toBeLessThanOrEqual(100);

    await expect(trang.locator('[data-ai-review] .badge')).toBeVisible();

    const dong = trang.locator('[data-ai-review] table tbody tr');
    const soTieuChi = await dong.count();
    expect(soTieuChi, 'không hiện bảng chi tiết tiêu chí').toBeGreaterThan(0);

    let tongTrongSo = 0;
    let cong = 0;
    for (let i = 0; i < soTieuChi; i++) {
      const o = dong.nth(i).locator('td');
      const w = Number((await o.nth(0).innerText()).match(/(\d+)%/)?.[1]);
      const s = Number((await o.nth(2).innerText()).trim());
      expect(Number.isFinite(w) && Number.isFinite(s), `dòng ${i} đọc không ra số`).toBe(true);
      tongTrongSo += w;
      cong += w * s;
    }

    expect(tongTrongSo, 'tổng trọng số trong ai-scoring.json phải bằng 100').toBe(100);
    expect(Math.abs(Math.round(cong / tongTrongSo) - diem), 'điểm tổng không khớp bảng chi tiết')
      .toBeLessThanOrEqual(1);
  });

  test('Chấm điểm được cả khách tiềm năng', async ({ trang }) => {
    await trang.goto('/khach-tiem-nang');

    // Kích vào dòng lưới KHÔNG mở gì — đó là quy ước của mọi lưới trong hệ thống này. Mở một cơ hội
    // bằng đường dẫn sâu ?mo={id}, đúng cách mà thông báo "@ nhắc bạn" dẫn người dùng tới.
    const id = await trang.evaluate(async () => {
      const r = await fetch('/khach-tiem-nang?handler=Data&draw=1&start=0&length=1');
      return (await r.json())?.data?.[0]?.id;
    });
    expect(id, 'không có cơ hội nào trong dữ liệu').toBeTruthy();

    await trang.goto(`/khach-tiem-nang?mo=${id}`);

    // Thẻ AI nằm trong panel sửa, chỉ hiện khi mở đúng một cơ hội đã có id.
    const the = trang.locator('#lead-review');
    await expect(the).not.toHaveClass(/d-none/, { timeout: 20_000 });
    await expect(the.locator('[data-act="Review"]')).toBeVisible({ timeout: 20_000 });
    await expect(the.locator('[data-act]')).toHaveCount(3);
  });

  /**
   * Chấm xong, tải lại trang thì điểm phải CÒN ĐÓ mà không phải bấm lại.
   *
   * Đây là toàn bộ lý do lưu kết quả. Bản trước không lưu gì: mở lại hồ sơ hôm sau là thẻ trống
   * trơn, muốn xem lại phải chạy lại — mất 15-20 giây và thêm một lượt gọi model có tính phí cho
   * một câu trả lời đã từng có.
   */
  test('Chấm điểm xong, tải lại trang vẫn thấy kết quả', async ({ trang }) => {
    const id = await moKhachCoTraoDoi(trang);
    await bam(trang, 'Review');

    const diemTruoc = (await trang.locator('[data-ai-review] .tk-ai-score').innerText()).trim();
    expect(diemTruoc.length, 'không đọc được điểm vừa chấm').toBeGreaterThan(0);

    await trang.goto(`/khach-hang/${id}`);

    // KHÔNG bấm gì cả — điểm phải tự hiện từ dữ liệu đã lưu.
    const diemSau = trang.locator('[data-ai-review] .tk-ai-score');
    await expect(diemSau).toBeVisible({ timeout: 30_000 });
    expect((await diemSau.innerText()).trim()).toBe(diemTruoc);

    // Và phải nói rõ chấm lúc nào. Thiếu dòng này thì điểm chấm từ tháng trước trông y hệt điểm
    // vừa chấm xong, người đọc ra quyết định trên hiện trạng đã cũ mà không hề biết.
    await expect(trang.locator('[data-ai-review]')).toContainText('Đã chấm lúc');

    await expect(trang.locator('[data-ai-review] [data-role="mo-lich-su"]')).toBeVisible();
  });

  /** Bấm "các lần trước" phải liệt kê được lịch sử, không phải một nút chết. */
  test('Xem được các lần chấm trước', async ({ trang }) => {
    const id = await moKhachCoTraoDoi(trang);

    // Chấm hai lần để chắc chắn có ít nhất một dòng lịch sử ngoài dòng đang hiện.
    await bam(trang, 'Review');
    await trang.goto(`/khach-hang/${id}`);
    await trang.locator('[data-ai-review] [data-act="Review"]').waitFor({ timeout: 30_000 });
    await bam(trang, 'Review');

    await trang.locator('[data-ai-review] [data-role="mo-lich-su"]').click();

    const ds = trang.locator('[data-ai-review] [data-role="history"]');
    await expect(ds).toContainText('Các lần trước', { timeout: 20_000 });
    await expect(ds.locator('li')).not.toHaveCount(0);
  });
});
