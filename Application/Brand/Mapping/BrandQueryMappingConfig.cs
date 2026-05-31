using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Application.Brand.Mapping;

public sealed class BrandQueryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BrandId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<CategoryId, Guid>()
            .MapWith(src => src.Value);
    }
}