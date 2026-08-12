import { test, expect } from '../fixtures.js';

/**
 * Khách hàng định danh bằng SỐ ĐIỆN THOẠI — mọi form liên quan phải tra số trước.
 *
 * Không tra thì mỗi form lại đẻ ra một khách trùng số, và tới lúc đối soát công nợ mới phát hiện
 * một người có ba hồ sơ. Lúc đó gộp lại rất tốn công vì đơn hàng đã bám vào cả ba.
 *
 * Ba vế phải đúng:
 *  - số ĐÃ CÓ  → gắn đúng hồ sơ, hiện tên để người dùng biết mình đang gắn ai;
 *  - số CHƯA CÓ → mời tạo nhanh, KHÔNG im lặng;
 *  - đổi từ số đã có sang số chưa có → phải BỎ liên kết cũ, không giữ lại khách trước đó.
 */

async function moThemCoHoi(page) {
  await page.goto('/co-hoi');
  await page.evaluate(() => window.oc.open(null));
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

/** Số của một khách CÓ THẬT trong dữ liệu, lấy từ chính màn khách hàng. */
async function sdtCoThat(page) {
  return page.evaluate(async () => {
    const r = await fetch('/khach-hang?handler=Data&draw=1&start=0&length=50');
    const j = await r.json();
    return (j.data || []).find((x) => x.phone && String(x.phone).replace(/\D/g, '').length >= 8)?.phone ?? null;
  });
}

test.describe('Tra khách theo số điện thoại', () => {
  test('Gõ số đã có thì gắn đúng hồ sơ và hiện tên', async ({ trang: page }) => {
    await page.goto('/co-hoi');
    const sdt = await sdtCoThat(page);
    test.skip(!sdt, 'Chưa có khách nào có số điện thoại để thử.');

    await moThemCoHoi(page);
    await page.locator('#frm [name="Input.ContactPhone"]').fill(sdt);

    const bao = page.locator('#frm .tk-khach-bao');
    await expect(bao, 'gõ số đã có mà không báo gì').toContainText('Đã có hồ sơ', { timeout: 10_000 });

    // Gắn được id thật thì lần lưu sau mới nối cơ hội vào đúng khách.
    const id = await page.locator('#frm [name="Input.CustomerId"]').inputValue();
    expect(id, 'không gắn được mã khách — cơ hội sẽ đứng rời khỏi hồ sơ khách').toBeTruthy();

    // Tên phải được điền hộ khi ô đang trống.
    await expect(page.locator('#frm [name="Input.ContactName"]')).not.toHaveValue('');
  });

  test('Gõ số chưa ai dùng thì mời tạo nhanh', async ({ trang: page }) => {
    await moThemCoHoi(page);

    await page.locator('#frm [name="Input.ContactPhone"]').fill('0999' + Date.now().toString().slice(-6));

    const bao = page.locator('#frm .tk-khach-bao');
    await expect(bao, 'số lạ mà im lặng — người dùng không biết phải làm gì').toContainText('Chưa có khách', { timeout: 10_000 });
    await expect(bao.locator('.tk-khach-tao'), 'không có nút tạo nhanh').toBeVisible();
  });

  test('Tạo nhanh mở hộp thoại, tạo xong thì gắn luôn khách vừa tạo', async ({ trang: page }) => {
    await moThemCoHoi(page);

    const sdt = '0988' + Date.now().toString().slice(-6);
    await page.locator('#frm [name="Input.ContactPhone"]').fill(sdt);

    const bao = page.locator('#frm .tk-khach-bao');
    await expect(bao.locator('.tk-khach-tao')).toBeVisible({ timeout: 10_000 });
    await bao.locator('.tk-khach-tao').click();

    // Tạo hồ sơ khách là việc có hệ quả lâu dài (mọi đơn hàng sau này bám vào nó), nên phải cho
    // người dùng nhìn thấy đủ thứ sắp lưu và sửa được — không tạo lén sau lưng một cú bấm.
    const hop = page.locator('#tk-khach-modal');
    await expect(hop, 'bấm Tạo nhanh mà không mở hộp thoại nào').toBeVisible({ timeout: 10_000 });

    // Số điện thoại phải được mang sẵn sang, khỏi gõ lại.
    await expect(page.locator('#tk-khach-sdt')).toHaveValue(sdt);

    await page.locator('#tk-khach-ten').fill('Khách kiểm thử ' + sdt.slice(-4));
    await page.locator('#tk-khach-luu').click();

    await expect(hop, 'tạo xong mà hộp thoại không đóng').toBeHidden({ timeout: 15_000 });
    await expect(bao, 'tạo nhanh xong mà trạng thái không đổi').toContainText('Đã có hồ sơ', { timeout: 15_000 });
    await expect(page.locator('#frm [name="Input.CustomerId"]'),
      'tạo xong mà không gắn khách vừa tạo — coi như chưa tạo').not.toHaveValue('');
  });

  test('Hộp tạo nhanh đòi tên khách, không cho tạo hồ sơ trống tên', async ({ trang: page }) => {
    await moThemCoHoi(page);
    await page.locator('#frm [name="Input.ContactPhone"]').fill('0966' + Date.now().toString().slice(-6));

    const bao = page.locator('#frm .tk-khach-bao');
    await expect(bao.locator('.tk-khach-tao')).toBeVisible({ timeout: 10_000 });
    await bao.locator('.tk-khach-tao').click();

    const hop = page.locator('#tk-khach-modal');
    await expect(hop).toBeVisible({ timeout: 10_000 });
    await page.locator('#tk-khach-ten').fill('');
    await page.locator('#tk-khach-luu').click();

    // Vẫn mở: một hồ sơ khách không tên là rác trong danh bạ, và không ai tra lại được nó.
    await expect(hop, 'tạo được hồ sơ khách không tên').toBeVisible();
  });

  test('Đổi sang số chưa có thì bỏ liên kết khách cũ', async ({ trang: page }) => {
    await page.goto('/co-hoi');
    const sdt = await sdtCoThat(page);
    test.skip(!sdt, 'Chưa có khách nào có số điện thoại để thử.');

    await moThemCoHoi(page);
    const $sdt = page.locator('#frm [name="Input.ContactPhone"]');
    const $id = page.locator('#frm [name="Input.CustomerId"]');

    await $sdt.fill(sdt);
    await expect($id).not.toHaveValue('', { timeout: 10_000 });

    // Giữ lại id cũ ở đây là gắn cơ hội vào NHẦM người — sai im lặng, không ô nào báo đỏ.
    await $sdt.fill('0977' + Date.now().toString().slice(-6));
    await expect($id, 'đổi số rồi mà vẫn giữ khách cũ').toHaveValue('', { timeout: 10_000 });
  });

  /** Tạo nhanh phải chặn ở SERVER, không chỉ ở nút bấm: số trùng vẫn không được tạo thêm hồ sơ. */
  test('Tạo nhanh trùng số bị server từ chối', async ({ trang: page }) => {
    await page.goto('/co-hoi');
    const sdt = await sdtCoThat(page);
    test.skip(!sdt, 'Chưa có khách nào có số điện thoại để thử.');

    const kq = await page.evaluate(async (so) => {
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const fd = new FormData();
      fd.append('fullName', 'Trùng số kiểm thử');
      fd.append('phone', so);
      const res = await fetch('/co-hoi?handler=TaoNhanhKhach', {
        method: 'POST', body: fd, headers: token ? { RequestVerificationToken: token } : {},
      });
      return await res.json();
    }, sdt);

    expect(kq.isSuccess, 'tạo được khách thứ hai cùng số — hỏng luật định danh').toBeFalsy();
    expect(String(kq.message)).toContain('Số điện thoại');
  });
});
