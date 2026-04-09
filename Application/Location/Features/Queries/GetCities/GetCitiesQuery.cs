using Application.Location.Features.Shared;

namespace Application.Location.Features.Queries.GetCities;

public record GetCitiesQuery(int StateId) : IRequest<ServiceResult<IEnumerable<CityDto>>>;