using Application.Common.Results;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutOrchestrationService
{
    Task<ServiceResult<CheckoutResultDto>> ProcessCheckoutAsync(
        CheckoutFromCartCommand command,
        CancellationToken ct);
}