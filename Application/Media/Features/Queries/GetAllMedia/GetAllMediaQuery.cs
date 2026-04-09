namespace Application.Media.Features.Queries.GetAllMedia;

public record GetAllMediaQuery(
    string? EntityType,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<MediaListItemDto>>>;