using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandler(
    ICheckoutOrchestrationService checkoutOrchestrationService)
    : IRequestHandler<CheckoutFromCartCommand, ServiceResult<CheckoutResultDto>>
{
    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request,
        CancellationToken ct)
    {
        return await checkoutOrchestrationService.ProcessCheckoutAsync(request, ct);
    }
}