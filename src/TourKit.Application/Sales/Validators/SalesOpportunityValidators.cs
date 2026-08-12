using FluentValidation;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales.Validators;

/// <summary>
/// Luật chung cho cả tạo lẫn sửa cơ hội, khai một lần.
///
/// Số lượng và giá KHÔNG được âm: cả hai đi thẳng vào công thức giá trị phễu, mà một dòng âm thì
/// tổng trên thẻ thống kê tụt xuống không có gì giải thích được — người xem sẽ đi tìm lỗi ở báo cáo
/// chứ không nghĩ tới một ô nhập sai dấu.
///
/// Không đặt trần cho giá: tour đoàn lớn có thể lên tới hàng tỉ, đặt trần là chặn nghiệp vụ thật.
/// </summary>
internal static class LuatCoHoi
{
    public static void ApDung<T>(AbstractValidator<T> v,
        Func<T, string> ma, Func<T, string> ten, Func<T, string> tenKhach, Func<T, string?> email,
        Func<T, int> nl, Func<T, int> te, Func<T, int> tn, Func<T, int> eb,
        Func<T, decimal> gNl, Func<T, decimal> gTe, Func<T, decimal> gTn, Func<T, decimal> gEb)
    {
        v.RuleFor(x => ma(x)).NotEmpty().WithMessage("Bắt buộc nhập mã cơ hội").MaximumLength(64);
        v.RuleFor(x => ten(x)).NotEmpty().WithMessage("Bắt buộc nhập tên cơ hội").MaximumLength(300);
        v.RuleFor(x => tenKhach(x)).NotEmpty().WithMessage("Bắt buộc nhập tên khách").MaximumLength(200);
        v.RuleFor(x => email(x)).EmailAddress().When(x => !string.IsNullOrWhiteSpace(email(x)));

        v.RuleFor(x => nl(x)).GreaterThanOrEqualTo(0).WithMessage("Số khách người lớn không được âm");
        v.RuleFor(x => te(x)).GreaterThanOrEqualTo(0).WithMessage("Số trẻ em không được âm");
        v.RuleFor(x => tn(x)).GreaterThanOrEqualTo(0).WithMessage("Số trẻ nhỏ không được âm");
        v.RuleFor(x => eb(x)).GreaterThanOrEqualTo(0).WithMessage("Số em bé không được âm");

        v.RuleFor(x => gNl(x)).GreaterThanOrEqualTo(0).WithMessage("Giá người lớn không được âm");
        v.RuleFor(x => gTe(x)).GreaterThanOrEqualTo(0).WithMessage("Giá trẻ em không được âm");
        v.RuleFor(x => gTn(x)).GreaterThanOrEqualTo(0).WithMessage("Giá trẻ nhỏ không được âm");
        v.RuleFor(x => gEb(x)).GreaterThanOrEqualTo(0).WithMessage("Giá em bé không được âm");
    }
}

public sealed class CreateSalesOpportunityValidator : AbstractValidator<CreateSalesOpportunityDto>
{
    public CreateSalesOpportunityValidator() => LuatCoHoi.ApDung(this,
        x => x.Code, x => x.Title, x => x.ContactName, x => x.ContactEmail,
        x => x.AdultQty, x => x.ChildQty, x => x.ChildSmallQty, x => x.BabyQty,
        x => x.PriceAdult, x => x.PriceChild, x => x.PriceChildSmall, x => x.PriceBaby);
}

public sealed class UpdateSalesOpportunityValidator : AbstractValidator<UpdateSalesOpportunityDto>
{
    public UpdateSalesOpportunityValidator() => LuatCoHoi.ApDung(this,
        x => x.Code, x => x.Title, x => x.ContactName, x => x.ContactEmail,
        x => x.AdultQty, x => x.ChildQty, x => x.ChildSmallQty, x => x.BabyQty,
        x => x.PriceAdult, x => x.PriceChild, x => x.PriceChildSmall, x => x.PriceBaby);
}
