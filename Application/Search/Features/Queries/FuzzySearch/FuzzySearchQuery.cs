using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.FuzzySearch;

public record FuzzySearchQuery(string Q) : IRequest<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>;