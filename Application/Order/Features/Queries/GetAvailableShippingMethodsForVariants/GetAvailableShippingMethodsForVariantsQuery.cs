namespace Application.Order.Features.Queries.GetAvailableShippingMethodsForVariants;

public record GetAvailableShippingMethodsForVariantsQuery(List<int> VariantIds) : IRequest<ServiceResult<IEnumerable<AvailableShippingMethodDto>>>;