using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingHandler(
    IShippingRepository shippingRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteShippingCommand>
{
    public async Task<ServiceResult> Handle(DeleteShippingCommand request, CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);

        var shipping = await shippingRepository.GetByIdAsync(shippingId, ct);
        if (shipping is null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        try
        {
            UserId? deletedBy = currentUser.UserId.HasValue
                ? UserId.From(currentUser.UserId.Value)
                : null;

            shipping.RequestDeletion(deletedBy);
            shippingRepository.Update(shipping);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}