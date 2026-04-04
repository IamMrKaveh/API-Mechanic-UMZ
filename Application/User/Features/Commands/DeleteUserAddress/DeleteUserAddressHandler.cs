using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressHandler(IUserRepository repo, IUnitOfWork uow) : IRequestHandler<DeleteUserAddressCommand, ServiceResult>
{
    private readonly IUserRepository _repo = repo;
    private readonly IUnitOfWork _uow = uow;

    public async Task<ServiceResult> Handle(
        DeleteUserAddressCommand request,
        CancellationToken ct)
    {
        var user = await _repo.GetWithAddressesAsync(request.UserId, ct);
        if (user == null)
            return ServiceResult.NotFound("User not found.");
        user.RemoveAddress(request.AddressId);
        _repo.Update(user);
        await _uow.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}