namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutShippingValidatorService
{
    Task<ServiceResult<Domain.Shipping.Aggregates.Shipping>> ValidateAsync(int shippingId, CancellationToken ct);
}