using Application.Common.Results;

namespace Application.Media.Features.Commands.DeleteMedia;

public record DeleteMediaCommand(Guid MediaId) : IRequest<ServiceResult>;