using Application.Common.Interfaces;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserById;

public class GetUserByIdHandler(
    IUserQueryService userQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUserByIdQuery, UserProfileDto?>
{
    public async Task<ServiceResult<UserProfileDto?>> Handle(
        GetUserByIdQuery request,
        CancellationToken ct)
    {
        // Users may only read their own profile; admins may read any.
        if (!currentUserService.IsAdmin && currentUserService.UserId != request.Id)
            return ServiceResult<UserProfileDto?>.Forbidden("دسترسی ممنوع.");

        var userId = UserId.From(request.Id);

        var dto = await userQueryService.GetUserProfileAsync(userId, ct);
        return dto.ToResultOrNullableNotFound("User not found");
    }
}