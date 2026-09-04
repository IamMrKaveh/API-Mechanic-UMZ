using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.ActivatePaymentMethod;

public sealed class ActivatePaymentMethodHandler(
    IPaymentMethodRepository repository,
    ICacheService cacheService)
    : ICommandHandler<ActivatePaymentMethodCommand>
{
    public async Task<ServiceResult> Handle(ActivatePaymentMethodCommand request, CancellationToken ct)
    {
        var id = PaymentMethodId.From(request.Id);
        var method = await repository.GetByIdAsync(id, ct);
        if (method is null)
            return ServiceResult.NotFound("روش پرداخت یافت نشد.");

        method.Activate();
        repository.Update(method);
        await cacheService.RemoveByPrefixAsync("payment-methods:", ct);

        return ServiceResult.Success();
    }
}