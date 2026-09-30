using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Search.Features.Queries.GetSearchSuggestions;

[RequestOutputCache(
    tags:
    [
        CacheTags.Product
    ],
    expirationInSeconds: 120)]
public record GetSearchSuggestionsQuery(
    string Q,
    int MaxSuggestions = 10)
    : IQuery<List<string>>;