namespace Presentation.Search.Requests;

public record SearchProductsRequest(
    string? Q = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Brand = null,
    bool InStockOnly = false,
    string? SortBy = null,
    string[]? Tags = null,
    int Page = 1,
    int PageSize = 20);

public record GetSearchSuggestionsRequest(
    string Q,
    int MaxSuggestions = 10);

public record FuzzySearchRequest(
    string Q,
    int Page = 1,
    int PageSize = 20);