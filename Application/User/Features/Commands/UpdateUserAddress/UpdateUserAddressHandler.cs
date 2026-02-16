namespace Application.User.Features.Commands.UpdateUserAddress;

public class UpdateUserAddressHandler : IRequestHandler<UpdateUserAddressCommand, ServiceResult>
{
    private readonly IUserRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateUserAddressHandler(IUserRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ServiceResult> Handle(UpdateUserAddressCommand request, CancellationToken ct)
    {
        var user = await _repo.GetWithAddressesAsync(request.UserId, ct);
        if (user == null) return ServiceResult.Failure("User not found.");
        user.UpdateAddress(request.AddressId, request.Dto.Title, request.Dto.ReceiverName, request.Dto.PhoneNumber, request.Dto.Province, request.Dto.City, request.Dto.Address, request.Dto.PostalCode, request.Dto.IsDefault);
        _repo.Update(user);
        await _uow.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}