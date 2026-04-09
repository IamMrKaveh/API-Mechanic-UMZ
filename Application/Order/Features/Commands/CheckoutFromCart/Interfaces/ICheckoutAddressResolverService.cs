using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutAddressResolverService
{
    Task<ServiceResult<(ReceiverInfo ReceiverInfo, DeliveryAddress DeliveryAddress)>> ResolveAsync(
        Guid userId,
        Guid addressId,
        CancellationToken ct);
}