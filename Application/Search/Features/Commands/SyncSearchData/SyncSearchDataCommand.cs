using Application.Common.Models;

namespace Application.Search.Features.Commands.SyncSearchData;

public record SyncSearchDataCommand : IRequest<ServiceResult>;