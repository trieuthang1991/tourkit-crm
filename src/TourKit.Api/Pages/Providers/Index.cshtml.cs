using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Import;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Providers;

// Danh sách NCC: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/providers/ProvidersPage.tsx): 3 thẻ thống kê, tab loại NCC, 7 tiêu chí lọc,
// cột ghép 2 dòng (NCC/liên hệ/email-địa chỉ/tổng mua-đã trả/đánh giá-trạng thái), dòng tổng cộng trang, export CSV.
[Authorize(Policy = "provider.view")]
public class IndexModel : TkListPageModel
{
    private readonly IProviderService _svc;
    private readonly IProviderServiceService _providerServices;
    private readonly IServiceItemService _serviceItems;
    private readonly IPaymentTermService _paymentTerms;
    private readonly IBranchService _branches;
    private readonly IMarketTypeService _marketTypes;
    private readonly ICurrencyService _currencies;
    private readonly TourKit.Api.Ai.AiBangGia _aiBangGia;

    public IndexModel(IProviderService svc, IProviderServiceService providerServices, IServiceItemService serviceItems,
        IPaymentTermService paymentTerms, IBranchService branches, IMarketTypeService marketTypes,
        ICurrencyService currencies,
        TourKit.Api.Ai.AiBangGia aiBangGia)
    {
        _svc = svc;
        _providerServices = providerServices;
        _serviceItems = serviceItems;
        _paymentTerms = paymentTerms;
        _branches = branches;
        _marketTypes = marketTypes;
        _currencies = currencies;
        _aiBangGia = aiBangGia;
    }

    public ProviderStatsDto Stats { get; private set; } = new(0, 0, 0);
    public IReadOnlyList<PaymentTermDto> PaymentTerms { get; private set; } = [];
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    public IReadOnlyList<MarketTypeDto> MarketTypes { get; private set; } = [];

    /// <summary>Danh mục tiền tệ cho ô chọn ở dòng bảng giá — mã tiền phải chọn, không gõ tay.</summary>
    public IReadOnlyList<CurrencyDto> Currencies { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    /// <summary>
    /// Các dòng sản phẩm/dịch vụ gửi lên CÙNG form sửa NCC — tái lập panel "SẢN PHẨM/DỊCH VỤ" của
    /// hệ cũ (EditHotel.aspx). Dòng cũ không nằm trong danh sách này nghĩa là người dùng đã bỏ nó.
    /// </summary>
    [BindProperty] public List<DongDichVuInput> Services { get; set; } = [];

    /// <summary>
    /// Danh sách người liên hệ gửi lên cùng form — tái lập khối "Thông tin liên hệ" lặp lại của hệ cũ.
    /// Lưu trong ProfileJson chứ không thành bảng riêng: đây là dữ liệu đi kèm NCC, không tra cứu độc lập.
    /// </summary>
    [BindProperty] public List<LienHeInput> Contacts { get; set; } = [];

    public sealed class LienHeInput
    {
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public sealed class DongDichVuInput
    {
        public Guid? Id { get; set; }
        public Guid? ServiceItemId { get; set; }
        public string? PriceName { get; set; }
        public decimal ContractPrice { get; set; }
        public decimal PublicPrice { get; set; }
        public string? CurrencyCode { get; set; }
        public int AmountOfPeople { get; set; }
        public string? Note { get; set; }
        public int Status { get; set; }

        // ----- Trường riêng theo loại NCC, lưu vào ProviderService.ProfileJson -----

        public DateOnly? PeriodFrom { get; set; }       // Khách sạn: "Giai đoạn từ"
        public DateOnly? PeriodTo { get; set; }         // Khách sạn: "Đến"
        public string? DayType { get; set; }            // Khách sạn: "Loại ngày"
        public decimal? NetCostPerDay { get; set; }     // Khách sạn: "Chi phí NET/Ngày"
        public decimal? SellPricePerDay { get; set; }   // Khách sạn: "Giá bán/Ngày"
        public string? TicketType { get; set; }         // Vé: "Loại vé"
        public string? Route { get; set; }              // Vé: "Hành trình vé"
        public string? DepartTime { get; set; }         // Vé: "Giờ đi"
        public string? ReturnTime { get; set; }         // Vé: "Giờ về"
        public DateOnly? DepositDeadline { get; set; }  // Vé: "Hạn cắt cọc"
        public string? Baggage { get; set; }            // Vé: "Hành lý"
    }

    /// <summary>
    /// Bảng dịch vụ của một NCC, để form sửa nạp sẵn khi mở. Kèm danh mục dịch vụ cho ô chọn —
    /// gửi chung một lượt để form không phải gọi hai lần.
    /// </summary>
    public async Task<IActionResult> OnGetServicesAsync(Guid providerId)
    {
        if (providerId == Guid.Empty)
        {
            return new JsonResult(Result.Success(null, new { lines = Array.Empty<object>(), items = Array.Empty<object>() }));
        }

        var ds = await _providerServices.ListAsync(1, MaxLines, providerId, null);
        var dm = await _serviceItems.ListAsync(1, LookupSize);

        return new JsonResult(Result.Success(null, new
        {
            lines = ds.Items.Select(x => new
            {
                id = x.Id,
                serviceItemId = x.ServiceItemId,
                priceName = x.PriceName,
                contractPrice = x.ContractPrice,
                publicPrice = x.PublicPrice,
                currencyCode = x.CurrencyCode,
                amountOfPeople = x.AmountOfPeople,
                note = x.Note,
                status = x.Status,
                // Trường riêng phải đi cùng dòng: form sửa nạp từ đây, thiếu là bấm Lưu ghi null đè lên.
                periodFrom = x.Profile?.PeriodFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                periodTo = x.Profile?.PeriodTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                dayType = x.Profile?.DayType,
                netCostPerDay = x.Profile?.NetCostPerDay,
                sellPricePerDay = x.Profile?.SellPricePerDay,
                ticketType = x.Profile?.TicketType,
                route = x.Profile?.Route,
                departTime = x.Profile?.DepartTime,
                returnTime = x.Profile?.ReturnTime,
                depositDeadline = x.Profile?.DepositDeadline?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                baggage = x.Profile?.Baggage,
            }),
            items = dm.Items.Select(x => new { id = x.Id, name = x.Name }),
        }));
    }

    // ===================== Nhập bảng giá từ tệp =====================

    /// <summary>
    /// Tải tệp mẫu ứng với loại NCC — cột riêng khác nhau nên mẫu cũng khác nhau.
    ///
    /// Tham số tên <c>loaiNcc</c> chứ KHÔNG phải <c>loai</c>: route của trang này đã có đoạn "loai"
    /// (/nha-cung-cap/loai/tat-ca), mà giá trị ROUTE thắng query string khi model binding. Đặt trùng
    /// tên thì tham số luôn nhận "tat-ca", không parse được số, và mẫu nào cũng trả về mẫu chung —
    /// hỏng im lặng, không có lỗi nào.
    /// </summary>
    public IActionResult OnGetMauNhap(int loaiNcc)
    {
        var t = Enum.IsDefined(typeof(ProviderType), loaiNcc) ? (ProviderType)loaiNcc : ProviderType.Other;
        var csv = MauNhapDichVu.MauCsv(t);

        // BOM UTF-8: thiếu nó là người dùng mở mẫu bằng Excel thấy tiêu đề tiếng Việt thành ký tự rác,
        // sửa lại rồi tải lên, và cột không khớp nữa.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"mau-bang-gia-{TypeSlug(t)}.csv");
    }

    private static string TypeSlug(ProviderType t) => t switch
    {
        ProviderType.Hotel => "khach-san",
        ProviderType.Airline => "hang-khong",
        _ => "chung",
    };

    /// <summary>
    /// Đọc tệp rồi trả về bảng XEM TRƯỚC — không ghi gì.
    ///
    /// Chủ dự án chốt luôn có bước xem trước: đọc sai một cột mà ghi thẳng thì bảng giá hỏng chỉ lộ ra
    /// khi có người phát hiện giá lệch, lúc đó đã dùng để báo giá cho khách rồi.
    /// </summary>
    public async Task<IActionResult> OnPostXemTruocNhapAsync(Guid providerId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return new JsonResult(Result.Error("Chưa chọn tệp."));
        }

        var ncc = await _svc.GetAsync(providerId);
        var cot = MauNhapDichVu.Cot(ncc.Type).Select(c => c.TieuDe).ToList();

        BangNhap bang;
        try
        {
            await using var s = file.OpenReadStream();

            if (DocChuTuTaiLieu.LaTaiLieu(file.FileName))
            {
                // Tài liệu báo giá: mỗi NCC trình bày một kiểu nên không có mẫu cố định — AI dựng lại
                // thành bảng theo đúng cột của mẫu, rồi đi tiếp qua CÙNG một lớp kiểm với đường CSV.
                var (bangAi, loiAi) = await _aiBangGia.DocAsync(
                    DocChuTuTaiLieu.Doc(s, file.FileName), cot, TourKit.Api.Ai.AiRecordAccess.ReadUserId(User) ?? Guid.Empty, HttpContext.RequestAborted);

                if (bangAi is null)
                {
                    return new JsonResult(Result.Error(loiAi ?? "Không đọc được tài liệu."));
                }

                bang = bangAi;
            }
            else
            {
                bang = DocBangTuTep.Doc(s, file.FileName);
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or IOException)
        {
            // Tệp hỏng/không đúng định dạng là lỗi NGƯỜI DÙNG, không phải sự cố hệ thống — nói rõ ra
            // thay vì để nó thành 500 "Đã có lỗi xảy ra".
            return new JsonResult(Result.Error(
                "Không đọc được tệp. Hãy dùng tệp .csv/.xlsx theo mẫu, hoặc .pdf/.docx là bản gốc (không phải ảnh scan)."));
        }

        var kq = new NhapDichVuService().XemTruoc(bang, ncc.Type);

        return new JsonResult(Result.Success(null, new
        {
            loaiNcc = TypeLabel(ncc.Type),
            nhan = kq.Nhan.Select(x => new
            {
                soDong = x.SoDong,
                tenGoiGia = x.Line!.PriceName,
                soKhach = x.Line.AmountOfPeople,
                giaHopDong = x.Line.ContractPrice,
                giaCongBo = x.Line.PublicPrice,
                tienTe = x.Line.CurrencyCode,
                ghiChu = x.Line.Note,
                hoSo = x.Line.Profile,
            }),
            hong = kq.Hong.Select(x => new { soDong = x.SoDong, tenGoiGia = x.TenGoiGia, loi = x.Loi }),
        }));
    }

    /// <summary>Ghi những dòng người dùng đã duyệt ở bảng xem trước. NỐI THÊM, không thay bảng giá đang có.</summary>
    public async Task<IActionResult> OnPostNhapAsync(Guid providerId, [FromBody] List<DongDichVuInput>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return new JsonResult(Result.Error("Không có dòng nào để nhập."));
        }

        var soDong = await _svc.ThemDichVuAsync(providerId, lines.Select(x => new ProviderServiceLineDto(
            null, null, x.PriceName, x.ContractPrice, x.PublicPrice, x.CurrencyCode,
            x.AmountOfPeople, x.Note, 1,
            new ProviderServiceLineProfile
            {
                PeriodFrom = x.PeriodFrom,
                PeriodTo = x.PeriodTo,
                DayType = Gon(x.DayType),
                NetCostPerDay = x.NetCostPerDay,
                SellPricePerDay = x.SellPricePerDay,
                TicketType = Gon(x.TicketType),
                Route = Gon(x.Route),
                DepartTime = Gon(x.DepartTime),
                ReturnTime = Gon(x.ReturnTime),
                DepositDeadline = x.DepositDeadline,
                Baggage = Gon(x.Baggage),
            })).ToList());

        return new JsonResult(Result.Success($"Đã nhập {soDong} dòng bảng giá."));
    }

    /// <summary>Trần số dòng nạp về form. Vượt mức này thì sửa hàng loạt ở màn Bảng giá NCC hợp lý hơn.</summary>
    private const int MaxLines = 200;

    /// <summary>Danh mục dịch vụ cho ô chọn — danh mục nhỏ, nạp có giới hạn chứ không get-all.</summary>
    private const int LookupSize = 500;

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public ProviderType Type { get; set; } = ProviderType.Hotel;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        public string? TaxCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public Guid? PaymentTermId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? MarketTypeId { get; set; }
        public int Rate { get; set; }
        public int Status { get; set; } = 1;

        // ----- Trường mềm, lưu gộp vào Provider.ProfileJson (xem ProviderProfile) -----

        public string? Website { get; set; }          // "Link" bên hệ cũ
        public string? BankAccountName { get; set; }  // "Tên TK"
        public string? Note { get; set; }             // "Ghi chú"
        public int? BuiltYear { get; set; }           // Khách sạn
        public string? Country { get; set; }          // Khách sạn
        public string? VehicleOwnership { get; set; } // Vận chuyển: xe nhà / xe đối tác
        public string? HotelClass { get; set; }       // Voucher: "Class Hotel"
        public string? ProjectName { get; set; }      // Voucher: "Tên dự án"
        public List<string> VehicleTypes { get; set; } = [];  // Vận chuyển: nhiều hạng ghế
    }

    /// <summary>Loại NCC — bám PROVIDER_TYPE của bản cũ (1..6).</summary>
    public static readonly (int Value, string Label)[] TypeOptions =
    [
        (1, "Khách sạn"), (2, "Vận chuyển"), (3, "Nhà hàng"), (4, "HDV"), (5, "Hàng không"),
        (7, "Voucher"), (6, "Khác"),
    ];

    public static string TypeLabel(ProviderType t) => t switch
    {
        ProviderType.Hotel => "Khách sạn",
        ProviderType.Vehicle => "Vận chuyển",
        ProviderType.Restaurant => "Nhà hàng",
        ProviderType.Guide => "HDV",
        ProviderType.Airline => "Hàng không",
        ProviderType.Voucher => "Voucher",
        ProviderType.Other => "Khác",
        _ => t.ToString(),
    };

    public static string StatusLabel(int s) => s == 1 ? "Hoạt động" : "Ngừng";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        PaymentTerms = await _paymentTerms.ListAsync();
        Branches = await _branches.ListAsync();
        MarketTypes = await _marketTypes.ListAsync();
        Currencies = await _currencies.ListAsync();
    }

    /// <summary>Dựng bộ lọc từ query — ĐỦ 8 tiêu chí của ProviderListFilter (không lọc ở client).</summary>
    private ProviderListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString().Trim();
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        // Mốc "đến": người dùng chọn NGÀY → lấy hết ngày đó.
        DateTimeOffset? DEnd(string k) => D(k) is { } d ? (d.TimeOfDay == TimeSpan.Zero ? d.AddDays(1).AddTicks(-1) : d) : null;

        // Loại NCC ưu tiên đoạn "loai" trong route (/nha-cung-cap/loai/hdv) rồi mới tới query ?type.
        int? loaiType = RouteData.Values.TryGetValue("loai", out var raw) && raw is string slug
            && TourKit.Api.Routing.RouteMap.ProviderLoai.TryGetValue(slug, out var t) ? t : null;

        return new ProviderListFilter(
            Q: keyword,
            Type: I("type") ?? loaiType,
            Status: I("status"),
            Province: S("province"),
            BranchId: G("branchId"),
            MarketTypeId: G("marketTypeId"),
            CreatedFrom: D("createdFrom"),
            CreatedTo: DEnd("createdTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + đủ field cho offcanvas sửa.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(p => new
        {
            id = p.Id,
            code = p.Code,
            name = p.Name,
            type = (int)p.Type,
            typeLabel = TypeLabel(p.Type),
            phone = p.Phone,
            email = p.Email,
            address = p.Address,
            province = p.Province,
            taxCode = p.TaxCode,
            contactPerson = p.ContactPerson,
            bankName = p.BankName,
            bankAccount = p.BankAccount,
            paymentTermId = p.PaymentTermId,
            branchId = p.BranchId,
            marketTypeId = p.MarketTypeId,
            rate = p.Rate,
            status = p.Status,
            statusLabel = StatusLabel(p.Status),
            // Trường mềm phải đi kèm dòng lưới, vì form sửa nạp từ chính dòng này. Thiếu ở đây là
            // mở sửa rồi bấm Lưu sẽ ghi null đè lên — đúng lỗi đã xảy ra ở màn chuyến đi.
            website = p.Profile?.Website,
            bankAccountName = p.Profile?.BankAccountName,
            note = p.Profile?.Note,
            builtYear = p.Profile?.BuiltYear,
            country = p.Profile?.Country,
            vehicleOwnership = p.Profile?.VehicleOwnership,
            hotelClass = p.Profile?.HotelClass,
            projectName = p.Profile?.ProjectName,
            contacts = (p.Profile?.Contacts ?? []).Select(c => new
            {
                fullName = c.FullName,
                position = c.Position,
                dateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                phone = c.Phone,
                email = c.Email,
            }),
            vehicleTypes = p.Profile?.VehicleTypes ?? [],
            totalCost = p.TotalCost,
            paid = p.Paid,
            outstanding = p.Outstanding,
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI — công nợ NCC (tổng mua · đã trả · còn nợ).
        var pageSum = new
        {
            totalCost = data.Sum(x => x.totalCost),
            paid = data.Sum(x => x.paid),
            outstanding = data.Sum(x => x.outstanding),
        };

        return GridJson(dt, stats.Total, result.Total, data,
            new Dictionary<string, object?> { ["pageSum"] = pageSum });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng để không sập).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã,Tên,Loại,Người liên hệ,SĐT,Email,Địa chỉ,Tỉnh thành,Tổng mua,Đã trả,Còn nợ,Đánh giá,Trạng thái");
        foreach (var p in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(p.Code)).Append(',').Append(C(p.Name)).Append(',').Append(C(TypeLabel(p.Type))).Append(',')
              .Append(C(p.ContactPerson)).Append(',').Append(C(p.Phone)).Append(',').Append(C(p.Email)).Append(',')
              .Append(C(p.Address)).Append(',').Append(C(p.Province)).Append(',')
              .Append(p.TotalCost.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(p.Paid.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(p.Outstanding.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(p.Rate.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(p.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "nha-cung-cap.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            // Sửa: ghi NCC và bảng dịch vụ trong MỘT lần — bám uspInsertHotel của hệ cũ, hỏng giữa
            // chừng thì không có gì được ghi. Tạo mới vẫn đi đường cũ vì form tạo chưa có panel dịch vụ.
            await _svc.UpdateWithServicesAsync(g, new UpdateProviderDto(
                Input.Name, Input.Type, Input.Phone, Input.Email, Input.Address, Input.TaxCode, NguoiLienHeChinh(),
                Input.BankAccount, Input.BankName, Input.PaymentTermId, Input.Rate, Input.Status,
                Province: Input.Province, BranchId: Input.BranchId, MarketTypeId: Input.MarketTypeId,
                Profile: HoSoTuInput()),
                Services.Select(x => new ProviderServiceLineDto(
                    x.Id, x.ServiceItemId, x.PriceName, x.ContractPrice, x.PublicPrice,
                    x.CurrencyCode, x.AmountOfPeople, x.Note, x.Status,
                    new ProviderServiceLineProfile
                    {
                        PeriodFrom = x.PeriodFrom,
                        PeriodTo = x.PeriodTo,
                        DayType = Gon(x.DayType),
                        NetCostPerDay = x.NetCostPerDay,
                        SellPricePerDay = x.SellPricePerDay,
                        TicketType = Gon(x.TicketType),
                        Route = Gon(x.Route),
                        DepartTime = Gon(x.DepartTime),
                        ReturnTime = Gon(x.ReturnTime),
                        DepositDeadline = x.DepositDeadline,
                        Baggage = Gon(x.Baggage),
                    })).ToList());
        }
        else
        {
            await _svc.CreateAsync(new CreateProviderDto(
                Input.Code, Input.Name, Input.Type, Input.Phone, Input.Email, Input.Address, Input.TaxCode, NguoiLienHeChinh(),
                Input.BankAccount, Input.BankName, Input.PaymentTermId, Input.Rate, Input.Status,
                Province: Input.Province, BranchId: Input.BranchId, MarketTypeId: Input.MarketTypeId,
                Profile: HoSoTuInput()));
        }

        return new JsonResult(Result.Success("Đã lưu nhà cung cấp."));
    }

    /// <summary>
    /// Gom trường mềm từ form thành <see cref="ProviderProfile"/>.
    ///
    /// Chỉ giữ trường của ĐÚNG loại NCC đang chọn: đổi khách sạn sang xe rồi lưu thì năm xây dựng
    /// phải biến mất theo, chứ không nằm lại trong JSON như rác vô hình mà giao diện không còn chỗ
    /// nào hiện ra để sửa hay xoá.
    /// </summary>
    /// <summary>Chuỗi rỗng/toàn khoảng trắng coi như không nhập — đừng lưu "" vào JSON.</summary>
    private static string? Gon(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Người liên hệ ghi vào CỘT <c>ContactPerson</c>.
    ///
    /// Cột này không thừa dù danh sách đã nằm trong JSON: lưới hiển thị nó và ô tìm kiếm dò theo nó
    /// ở SQL, mà tìm bên trong JSON là quét cả bảng. Nếu người dùng chỉ khai danh sách mà bỏ trống ô
    /// này thì lấy người ĐẦU TIÊN — bỏ trống sẽ khiến NCC biến mất khỏi kết quả tìm theo tên liên hệ.
    /// </summary>
    private string? NguoiLienHeChinh() =>
        Gon(Input.ContactPerson) ?? Contacts.Select(c => Gon(c.FullName)).FirstOrDefault(x => x is not null);

    private ProviderProfile HoSoTuInput()
    {
        var ks = ProviderProfile.CoTruongKhachSan(Input.Type);
        var xe = ProviderProfile.CoTruongXe(Input.Type);
        var vc = ProviderProfile.CoTruongVoucher(Input.Type);

        return new ProviderProfile
        {
            Website = Gon(Input.Website),
            BankAccountName = Gon(Input.BankAccountName),
            Note = Gon(Input.Note),
            BuiltYear = ks ? Input.BuiltYear : null,
            Country = ks ? Gon(Input.Country) : null,
            VehicleOwnership = xe ? Gon(Input.VehicleOwnership) : null,
            HotelClass = vc ? Gon(Input.HotelClass) : null,
            ProjectName = vc ? Gon(Input.ProjectName) : null,
            // Bỏ dòng trống: người dùng bấm "Thêm người" rồi không điền gì là chuyện thường, lưu lại
            // thì lần mở sau form đầy dòng rỗng và số người liên hệ đếm sai.
            Contacts = Contacts
                .Select(x => new ProviderContact
                {
                    FullName = Gon(x.FullName),
                    Position = Gon(x.Position),
                    DateOfBirth = x.DateOfBirth,
                    Phone = Gon(x.Phone),
                    Email = Gon(x.Email),
                })
                .Where(x => !x.Rong)
                .ToList(),
            VehicleTypes = xe ? Input.VehicleTypes.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() : [],
        };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá nhà cung cấp.";
        return RedirectToPage();
    }

    /// <summary>Xoá nhiều NCC đã chọn (tác vụ hàng loạt của tk.grid) — AJAX, trả Result.</summary>
    public async Task<IActionResult> OnPostBulkDeleteAsync([FromForm] Guid[] ids)
    {
        if (ids is null || ids.Length == 0)
        {
            return new JsonResult(Result.Error("Chưa chọn nhà cung cấp nào."));
        }

        foreach (var id in ids)
        {
            await _svc.DeleteAsync(id);
        }

        return new JsonResult(Result.Success($"Đã xoá {ids.Length} nhà cung cấp."));
    }
}
