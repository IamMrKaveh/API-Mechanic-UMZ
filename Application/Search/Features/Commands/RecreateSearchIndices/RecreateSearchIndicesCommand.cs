using Application.Common.Results;

namespace Application.Search.Features.Commands.RecreateSearchIndices;

public record RecreateSearchIndicesCommand : IRequest<ServiceResult>;