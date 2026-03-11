using Application.Common.Models;

namespace Application.User.Features.Queries.GetUserById;

public class GetUserByIdHandler(IUserQueryService userQueryService) : IRequestHandler<GetUserByIdQuery, ServiceResult<UserProfileDto?>>
{
    private readonly IUserQueryService _userQueryService = userQueryService;

    public async Task<ServiceResult<UserProfileDto?>> Handle(
        GetUserByIdQuery request,
        CancellationToken ct)
    {
        var dto = await _userQueryService.GetUserProfileAsync(request.Id, ct);
        return dto == null
            ? ServiceResult<UserProfileDto?>.Failure("User not found")
            : ServiceResult<UserProfileDto?>.Success(dto);
    }
}