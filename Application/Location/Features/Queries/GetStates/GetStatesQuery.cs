using Application.Location.Features.Shared;

namespace Application.Location.Features.Queries.GetStates;

public record GetStatesQuery : IRequest<ServiceResult<IEnumerable<StateDto>>>;