using Application.Common.Models;

namespace Application.Media.Features.Commands.SetPrimaryMedia;

public record SetPrimaryMediaCommand(int MediaId) : IRequest<ServiceResult>;