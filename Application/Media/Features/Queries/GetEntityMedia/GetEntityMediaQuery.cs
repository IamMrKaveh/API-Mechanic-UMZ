namespace Application.Media.Features.Queries.GetEntityMedia;

public record GetEntityMediaQuery(
    string EntityType,
    int EntityId) : IRequest<ServiceResult<IReadOnlyList<MediaDto>>>;