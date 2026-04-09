using Domain.Common.Exceptions;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingHandler(
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteShippingCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteShippingCommand request, CancellationToken ct)
    {
        var shipping = await shippingRepository.GetByIdAsync(ShippingId.From(request.Id), ct);
        if (shipping is null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        try
        {
            UserId? deletedBy = request.DeletedByUserId.HasValue
                ? UserId.From(request.DeletedByUserId.Value)
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