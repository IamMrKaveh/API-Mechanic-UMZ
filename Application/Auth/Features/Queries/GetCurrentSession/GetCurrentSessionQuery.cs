using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Queries.GetCurrentSession;

public record GetCurrentSessionQuery : IQuery<CurrentSessionDto>;