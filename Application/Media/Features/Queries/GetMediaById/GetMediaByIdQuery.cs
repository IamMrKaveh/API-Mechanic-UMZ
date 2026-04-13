using Application.Media.Features.Shared;

namespace Application.Media.Features.Queries.GetMediaById;

public record GetMediaByIdQuery(Guid MediaId) : IRequest<ServiceResult<MediaDto>>;