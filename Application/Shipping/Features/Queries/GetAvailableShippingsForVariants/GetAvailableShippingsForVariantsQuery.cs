namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public record GetAvailableShippingsForVariantsQuery(ICollection<Guid> VariantIds) : IRequest<ServiceResult<IEnumerable<AvailableShippingDto>>>;