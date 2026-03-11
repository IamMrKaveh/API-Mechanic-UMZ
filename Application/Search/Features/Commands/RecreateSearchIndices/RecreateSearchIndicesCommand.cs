using Application.Common.Models;

namespace Application.Search.Features.Commands.RecreateSearchIndices;

public record RecreateSearchIndicesCommand : IRequest<ServiceResult>;