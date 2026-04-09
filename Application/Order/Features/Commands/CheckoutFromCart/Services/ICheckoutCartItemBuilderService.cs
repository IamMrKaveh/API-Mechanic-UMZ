namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutCartItemBuilderService
{
    Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(Guid cartId, Guid userId, CancellationToken ct);
}