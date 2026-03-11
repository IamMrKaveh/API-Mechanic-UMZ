namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutDiscountApplicatorService
{
    Task<ServiceResult> ApplyAsync(
        Domain.Order.Aggregates.Order order,
        string? discountCode,
        int userId,
        CancellationToken ct);
}