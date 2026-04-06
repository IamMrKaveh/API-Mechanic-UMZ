using Application.Common.Results;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutAddressResolverService
{
    Task<ServiceResult<(ReceiverInfo ReceiverInfo, DeliveryAddress DeliveryAddress)>> ResolveAsync(
        Guid userId,
        Guid addressId,
        CancellationToken ct);
}