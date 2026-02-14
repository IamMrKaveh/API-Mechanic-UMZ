namespace Application.User.Features.Commands.CreateUser;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, ServiceResult<(UserProfileDto? User, string? Error)>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResult<(UserProfileDto? User, string? Error)>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await _userRepository.PhoneNumberExistsAsync(request.User.PhoneNumber, request.User.Id, ct))
        {
            return ServiceResult<(UserProfileDto?, string?)>.Failure("User with this phone number already exists.");
        }

        await _userRepository.AddUserAsync(request.User);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<UserProfileDto>(request.User);
        return ServiceResult<(UserProfileDto?, string?)>.Success((dto, null));
    }
}