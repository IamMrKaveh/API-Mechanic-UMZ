using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetUserDashboard;

public record GetUserDashboardQuery(Guid UserId) : IRequest<ServiceResult<UserDashboardDto>>;