using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserDashboard;

public record GetUserDashboardQuery(UserId UserId) : IRequest<ServiceResult<UserDashboardDto>>;