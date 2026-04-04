using Application.Common.Results;
using Domain.Media.ValueObjects;

namespace Application.Media.Features.Commands.DeleteMedia;

public record DeleteMediaCommand(MediaId Id, int? DeletedBy = null) : IRequest<ServiceResult>;