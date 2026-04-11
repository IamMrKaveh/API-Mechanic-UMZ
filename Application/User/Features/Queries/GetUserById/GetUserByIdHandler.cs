using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserById;

public class GetUserByIdHandler(IUserQueryService userQueryService) : IRequestHandler<GetUserByIdQuery, ServiceResult<UserProfileDto?>>
{
    public async Task<ServiceResult<UserProfileDto?>> Handle(
        GetUserByIdQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);

        var dto = await userQueryService.GetUserProfileAsync(userId, ct);
        return dto is null
            ? ServiceResult<UserProfileDto?>.NotFound("User not found")
            : ServiceResult<UserProfileDto?>.Success(dto);
    }
}