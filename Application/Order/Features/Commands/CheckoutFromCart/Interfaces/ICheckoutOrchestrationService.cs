using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutOrchestrationService
{
    Task<ServiceResult<CheckoutResultDto>> ProcessCheckoutAsync(
        CheckoutFromCartCommand command,
        CancellationToken ct);
}