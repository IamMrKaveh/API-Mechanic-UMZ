namespace Application.Media.Features.Queries.GetAllMedia;

public record GetAllMediaQuery(string? EntityType) : IRequest<ServiceResult<PaginatedResult<MediaListItemDto>>>;