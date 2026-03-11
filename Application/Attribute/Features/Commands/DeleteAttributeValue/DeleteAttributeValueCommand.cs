using Application.Common.Models;

namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public record DeleteAttributeValueCommand(
    int Id
    ) : IRequest<ServiceResult>;