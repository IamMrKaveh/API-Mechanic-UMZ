using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.DeactivatePaymentMethod;

public sealed class DeactivatePaymentMethodHandler(
    IPaymentMethodRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivatePaymentMethodCommand>
{
    public async Task<ServiceResult> Handle(DeactivatePaymentMethodCommand request, CancellationToken ct)
    {
        var id = PaymentMethodId.From(request.Id);
        var method = await repository.GetByIdAsync(id, ct);
        if (method is null)
            return ServiceResult.NotFound("روش پرداخت یافت نشد.");

        method.Deactivate();
        repository.Update(method);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}