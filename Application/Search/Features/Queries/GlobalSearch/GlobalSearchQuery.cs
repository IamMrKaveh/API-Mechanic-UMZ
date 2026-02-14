namespace Application.Search.Features.Queries.GlobalSearch;

public record GlobalSearchQuery(string Q)
    : IRequest<ServiceResult<GlobalSearchResultDto>>;