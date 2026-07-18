using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart;

public sealed class CheckoutFromCartHandler(
    ICheckoutOrchestrationService checkoutOrchestrationService,
    ICurrentUserService currentUserService)
    : ICommandHandler<CheckoutFromCartCommand, CheckoutResultDto>
{
    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request,
        CancellationToken ct)
    {
        if (!currentUserService.UserId.HasValue)
            return ServiceResult<CheckoutResultDto>.Unauthorized("کاربر احراز هویت نشده است.");

        var enriched = request with
        {
            UserId = currentUserService.UserId.Value,
            IpAddress = currentUserService.IpAddress ?? string.Empty,
            UserAgent = currentUserService.UserAgent
        };

        return await checkoutOrchestrationService.ProcessCheckoutAsync(enriched, ct);
    }
}