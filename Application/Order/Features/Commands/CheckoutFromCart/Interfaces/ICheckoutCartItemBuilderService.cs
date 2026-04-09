namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutCartItemBuilderService
{
    Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(Guid cartId, Guid userId, CancellationToken ct);
}