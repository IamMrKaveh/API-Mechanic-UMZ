using Application.Common.Results;
using Domain.Common.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutShippingValidatorService
{
    Task<ServiceResult<Money>> ValidateAndCalculateCostAsync(
        Guid shippingId,
        decimal orderAmount,
        CancellationToken ct);
}