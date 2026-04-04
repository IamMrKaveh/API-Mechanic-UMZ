using Application.Common.Results;

namespace Application.Media.Features.Queries.GetAllMedia;

public record GetAllMediaQuery(
    string? EntityType,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<MediaListItemDto>>>;