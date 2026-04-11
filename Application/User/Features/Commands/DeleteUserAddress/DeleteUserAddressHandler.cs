using Domain.Common.Exceptions;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserAddressCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteUserAddressCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var addressId = UserAddressId.From(request.AddressId);

        var user = await userRepository.GetWithAddressesAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        try
        {
            user.RemoveAddress(addressId);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}