using Application.User.Features.Shared;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateUserCommand, ServiceResult<UserProfileDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public async Task<ServiceResult<UserProfileDto>> Handle(
        CreateUserCommand request,
        CancellationToken ct)
    {
        if (await _userRepository.PhoneNumberExistsAsync(request.Dto.PhoneNumber, 0, ct))
            return ServiceResult<UserProfileDto>.Conflict("User with this phone number already exists.");

        var user = Domain.User.Aggregates.User.Create(request.Dto.PhoneNumber);

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto>.Success(dto);
    }
}