using Application.Common.Results;
using Application.Order.Features.Commands.CheckoutFromCart.Services;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandler(
    ICheckoutOrchestrationService orchestrationService,
    ILogger<CheckoutFromCartHandler> logger) : IRequestHandler<CheckoutFromCartCommand, ServiceResult<CheckoutResultDto>>
{
    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request, CancellationToken ct)
    {
        logger.LogInformation(
            "Checkout initiated for user {UserId}, cart {CartId}",
            request.UserId, request.CartId);

        return await orchestrationService.ProcessCheckoutAsync(request, ct);
    }
}