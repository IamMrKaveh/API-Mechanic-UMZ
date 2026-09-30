using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.DeactivatePaymentMethod;

public sealed class DeactivatePaymentMethodHandler(
    IPaymentMethodRepository repository)
    : ICommandHandler<DeactivatePaymentMethodCommand>
{
    public async Task<ServiceResult> Handle(DeactivatePaymentMethodCommand request, CancellationToken ct)
    {
        var id = PaymentMethodId.From(request.Id);
        var methodResult = await (repository.GetByIdAsync(id, ct)).OrNotFoundAsync("روش پرداخت یافت نشد.");
        if (methodResult.IsFailure) return methodResult.Error;
        var method = methodResult.Value;

        method.Deactivate();
        repository.Update(method);

        return ServiceResult.Success();
    }
}