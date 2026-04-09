using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.FuzzySearch;

public record FuzzySearchQuery(
    string Q,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>;