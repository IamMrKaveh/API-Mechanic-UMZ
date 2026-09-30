using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingHandler(
    IShippingRepository shippingRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<DeleteShippingCommand>
{
    public async Task<ServiceResult> Handle(DeleteShippingCommand request, CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);

        var shippingResult = await (shippingRepository.GetByIdAsync(shippingId, ct)).OrNotFoundAsync("روش ارسال یافت نشد.");
        if (shippingResult.IsFailure) return shippingResult.Error;
        var shipping = shippingResult.Value;

        try
        {
            UserId? deletedBy = currentUser.UserId.HasValue
                ? UserId.From(currentUser.UserId.Value)
                : null;

            shipping.RequestDeletion(deletedBy);
            shippingRepository.Update(shipping);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}