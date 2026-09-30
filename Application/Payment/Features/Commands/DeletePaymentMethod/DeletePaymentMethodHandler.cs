using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Features.Commands.DeletePaymentMethod;

public sealed class DeletePaymentMethodHandler(
    IPaymentMethodRepository repository,
    ICurrentUserService currentUser)
    : ICommandHandler<DeletePaymentMethodCommand>
{
    public async Task<ServiceResult> Handle(DeletePaymentMethodCommand request, CancellationToken ct)
    {
        var id = PaymentMethodId.From(request.Id);
        var methodResult = await (repository.GetByIdAsync(id, ct)).OrNotFoundAsync("روش پرداخت یافت نشد.");
        if (methodResult.IsFailure) return methodResult.Error;
        var method = methodResult.Value;

        try
        {
            UserId? deletedBy = currentUser.UserId.HasValue
                ? UserId.From(currentUser.UserId.Value)
                : null;

            method.RequestDeletion(deletedBy);
            repository.Update(method);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}