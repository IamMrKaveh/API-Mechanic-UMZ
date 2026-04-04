using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserAddresses;

public record GetUserAddressesQuery(UserId UserId) : IRequest<ServiceResult<IEnumerable<UserAddressDto>>>;