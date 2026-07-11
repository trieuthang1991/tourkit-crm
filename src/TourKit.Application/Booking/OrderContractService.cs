using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Gom dữ liệu in hợp đồng tour (legacy contract_tour) từ đơn + khách + chuyến + mẫu tour (điều khoản).
/// Số khách = tổng các dòng chỗ của đơn. Giá = Order.TotalRevenue (đã chốt).
/// </summary>
public sealed class OrderContractService(
    IRepository<Order> orderRepo,
    IRepository<Customer> customerRepo,
    IRepository<TourDeparture> departureRepo,
    IRepository<TourTemplate> templateRepo,
    IRepository<TourCustomer> seatRepo) : IOrderContractService
{
    public async Task<OrderContractDto> GetAsync(Guid orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId) ?? throw new NotFoundException();
        var customer = await customerRepo.GetByIdAsync(order.CustomerId) ?? throw new NotFoundException();
        var departure = await departureRepo.GetByIdAsync(order.TourDepartureId) ?? throw new NotFoundException();

        string? terms = null;
        if (departure.ParentTourId is { } tplId)
        {
            terms = (await templateRepo.GetByIdAsync(tplId))?.TermsNote;
        }

        var seats = await seatRepo.ListAsync(s => s.OrderId == orderId);
        var adults = seats.Sum(s => s.Quantity);
        var children = seats.Sum(s => s.AmountChildren + s.AmountChildrenSmall);
        var infants = seats.Sum(s => s.QuantityBaby);

        return new OrderContractDto(
            order.Code,
            customer.FullName, customer.Phone, customer.Address, customer.IdCardNumber, customer.PassportNumber,
            departure.Title, departure.DepartureDate, departure.EndDate,
            adults, children, infants,
            order.TotalRevenue, terms);
    }
}
