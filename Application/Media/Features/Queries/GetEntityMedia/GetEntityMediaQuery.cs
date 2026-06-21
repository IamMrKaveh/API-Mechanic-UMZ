using Application.Media.Features.Shared;

namespace Application.Media.Features.Queries.GetEntityMedia;

public record GetEntityMediaQuery(
    string EntityType,
    Guid EntityId) : IQuery<IReadOnlyList<MediaDto>>;