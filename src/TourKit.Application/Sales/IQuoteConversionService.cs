using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales;

public interface IQuoteConversionService
{
    /// <summary>Chuyển báo giá ĐÃ CHẤP NHẬN thành Order (+ ServiceBooking cho dòng dịch vụ đặt ngoài).</summary>
    Task<ConvertQuoteResultDto> ConvertAsync(Guid quoteId, ConvertQuoteDto dto);
}
