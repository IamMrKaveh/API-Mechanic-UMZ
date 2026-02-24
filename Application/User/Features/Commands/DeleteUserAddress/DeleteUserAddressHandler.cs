namespace Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressHandler : IRequestHandler<DeleteUserAddressCommand, ServiceResult>
{
    private readonly IUserRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteUserAddressHandler(IUserRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ServiceResult> Handle(DeleteUserAddressCommand request, CancellationToken ct)
    {
        var user = await _repo.GetWithAddressesAsync(request.UserId, ct);
        if (user == null) return ServiceResult.Failure("User not found.");
        user.RemoveAddress(request.AddressId, request.UserId);
        _repo.Update(user);
        await _uow.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}