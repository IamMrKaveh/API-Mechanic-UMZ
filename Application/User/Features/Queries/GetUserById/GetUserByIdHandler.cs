using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserById;

public class GetUserByIdHandler(
    IUserQueryService userQueryService)
    : IQueryHandler<GetUserByIdQuery, UserProfileDto?>
{
    public async Task<ServiceResult<UserProfileDto?>> Handle(
        GetUserByIdQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);

        var dto = await userQueryService.GetUserProfileAsync(userId, ct);
        return dto.ToResultOrNotFound("User not found");
    }
}