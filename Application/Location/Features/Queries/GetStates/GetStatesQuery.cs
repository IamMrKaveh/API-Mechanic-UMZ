using Application.Location.Features.Shared;

namespace Application.Location.Features.Queries.GetStates;

public record GetStatesQuery(
    int Page = 1,
    int PageSize = 50) : IPageQuery<ProvinceDto>;