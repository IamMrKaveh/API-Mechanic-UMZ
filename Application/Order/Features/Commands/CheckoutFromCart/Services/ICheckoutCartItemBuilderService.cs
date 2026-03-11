namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutCartItemBuilderService
{
    Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(Domain.Cart.Aggregates.Cart cart, CancellationToken ct);
}