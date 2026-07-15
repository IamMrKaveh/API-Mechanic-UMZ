using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.ActivatePaymentMethod;

public sealed class ActivatePaymentMethodHandler(
    IPaymentMethodRepository repository)
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

        return ServiceResult.Success();
    }
}