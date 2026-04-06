using Application.Common.Results;

namespace Application.Brand.Features.Commands.DeleteBrand;

public record DeleteBrandCommand(Guid Id) : IRequest<ServiceResult>;