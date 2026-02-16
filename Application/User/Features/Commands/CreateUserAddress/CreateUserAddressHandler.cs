namespace Application.User.Features.Commands.CreateUserAddress;

public class CreateUserAddressHandler : IRequestHandler<CreateUserAddressCommand, ServiceResult<int>>
{
    private readonly IUserRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateUserAddressHandler(IUserRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ServiceResult<int>> Handle(CreateUserAddressCommand request, CancellationToken ct)
    {
        var user = await _repo.GetWithAddressesAsync(request.UserId, ct);
        if (user == null) return ServiceResult<int>.Failure("User not found.");
        var addr = user.AddAddress(request.Dto.Title, request.Dto.ReceiverName, request.Dto.PhoneNumber, request.Dto.Province, request.Dto.City, request.Dto.Address, request.Dto.PostalCode, request.Dto.IsDefault);
        _repo.Update(user);
        await _uow.SaveChangesAsync(ct);
        return ServiceResult<int>.Success(addr.Id);
    }
}