using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Features.Commands.DeletePaymentMethod;

public sealed class DeletePaymentMethodHandler(
    IPaymentMethodRepository repository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeletePaymentMethodCommand>
{
    public async Task<ServiceResult> Handle(DeletePaymentMethodCommand request, CancellationToken ct)
    {
        var id = PaymentMethodId.From(request.Id);
        var method = await repository.GetByIdAsync(id, ct);
        if (method is null)
            return ServiceResult.NotFound("روش پرداخت یافت نشد.");

        try
        {
            UserId? deletedBy = currentUser.UserId.HasValue
                ? UserId.From(currentUser.UserId.Value)
                : null;

            method.RequestDeletion(deletedBy);
            repository.Update(method);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}