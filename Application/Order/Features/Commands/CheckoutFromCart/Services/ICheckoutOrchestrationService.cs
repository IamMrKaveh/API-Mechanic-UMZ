namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutOrchestrationService
{
    Task<ServiceResult<CheckoutResultDto>> OrchestrateAsync(
        CheckoutFromCartCommand command,
        CancellationToken ct);
}