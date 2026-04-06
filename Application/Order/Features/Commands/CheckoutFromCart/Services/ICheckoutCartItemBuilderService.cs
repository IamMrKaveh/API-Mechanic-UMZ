using Application.Common.Results;
using Application.Order.Features.Commands.CheckoutFromCart.Services;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutCartItemBuilderService
{
    Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(Guid cartId, Guid userId, CancellationToken ct);
}