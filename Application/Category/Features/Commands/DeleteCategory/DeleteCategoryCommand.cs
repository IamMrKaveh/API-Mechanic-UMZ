using Application.Common.Results;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(CategoryId Id) : IRequest<ServiceResult>;