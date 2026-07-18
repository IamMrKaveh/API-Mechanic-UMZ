using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Queries.GetCurrentSession;

public sealed class GetCurrentSessionHandler(ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentSessionQuery, CurrentSessionDto>
{
    public Task<ServiceResult<CurrentSessionDto>> Handle(
        GetCurrentSessionQuery request,
        CancellationToken ct)
    {
        var dto = new CurrentSessionDto
        {
            SessionId = currentUserService.SessionId,
            UserId = currentUserService.UserId,
            IpAddress = currentUserService.IpAddress,
            UserAgent = currentUserService.UserAgent,
            IsAuthenticated = currentUserService.IsAuthenticated,
            IsAdmin = currentUserService.IsAdmin
        };

        return Task.FromResult(ServiceResult<CurrentSessionDto>.Success(dto));
    }
}