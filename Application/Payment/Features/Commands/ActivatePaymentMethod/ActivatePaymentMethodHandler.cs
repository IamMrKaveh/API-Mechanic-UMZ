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
        var methodResult = await (repository.GetByIdAsync(id, ct)).OrNotFoundAsync("روش پرداخت یافت نشد.");
        if (methodResult.IsFailure) return methodResult.Error;
        var method = methodResult.Value;

        method.Activate();
        repository.Update(method);

        return ServiceResult.Success();
    }
}