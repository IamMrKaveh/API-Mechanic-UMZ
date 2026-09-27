using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<DeleteUserAddressCommand>
{
    public async Task<ServiceResult> Handle(DeleteUserAddressCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUser.UserId!.Value);
        var addressId = UserAddressId.From(request.AddressId);

        var userResult = await (userRepository.GetWithAddressesAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        try
        {
            user.RemoveAddress(addressId);
            userRepository.Update(user);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}