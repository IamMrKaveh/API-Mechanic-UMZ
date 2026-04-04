using Application.Common.Results;
using Application.Media.Features.Shared;

namespace Application.Media.Features.Queries.GetMediaById;

public record GetMediaByIdQuery(int MediaId)
    : IRequest<ServiceResult<MediaDetailDto?>>;