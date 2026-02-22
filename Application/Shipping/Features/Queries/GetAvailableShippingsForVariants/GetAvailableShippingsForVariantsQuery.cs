namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public record GetAvailableShippingsForVariantsQuery(List<int> VariantIds) : IRequest<ServiceResult<IEnumerable<AvailableShippingDto>>>;