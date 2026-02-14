namespace Application.Search.Features.Queries.FuzzySearch;

public record FuzzySearchQuery(string Q, int Page = 1, int PageSize = 20)
    : IRequest<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>;