using Application.Common.Results;
using Application.Order.Features.Commands.CheckoutFromCart.Services;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandler(
    ICheckoutOrchestrationService orchestrationService,
    ILogger<CheckoutFromCartHandler> logger) : IRequestHandler<CheckoutFromCartCommand, ServiceResult<CheckoutResultDto>>
{
    private readonly ICheckoutOrchestrationService _orchestrationService = orchestrationService;
    private readonly ILogger<CheckoutFromCartHandler> _logger = logger;

    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Checkout initiated for user {UserId} with idempotency key {Key}",
            request.UserId, request.IdempotencyKey);

        return await _orchestrationService.OrchestrateAsync(request, ct);
    }
}