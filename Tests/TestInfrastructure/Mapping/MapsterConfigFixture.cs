using Application.Category.Mapping;
using Mapster;

namespace Tests.TestInfrastructure.Mapping;

public sealed class MapsterConfigFixture
{
    public IMapper Mapper { get; }

    public MapsterConfigFixture()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(CategoryMappingConfig).Assembly);
        Mapper = new Mapper(config);
    }
}
