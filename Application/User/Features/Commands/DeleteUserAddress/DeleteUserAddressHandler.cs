using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserAddressCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteUserAddressCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetWithAddressesAsync(UserId.From(request.UserId), ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        try
        {
            user.RemoveAddress(UserAddressId.From(request.AddressId));
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