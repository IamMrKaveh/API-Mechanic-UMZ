using Application.Shipping.Mapping;
using Mapster;

namespace Tests.TestInfrastructure.Fixtures;

public sealed class AutoMapperFixture
{
    public IMapper Mapper { get; }

    public AutoMapperFixture()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ShippingMappingConfig).Assembly);
        Mapper = new Mapper(config);
    }
}
