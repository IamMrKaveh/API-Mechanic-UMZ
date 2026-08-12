using Application.Category.Mapping;
using Mapster;

namespace Tests.TestInfrastructure.Mapping;

public sealed class MapsterConfigFixture
{
    public MapsterConfigFixture()
    {
        new CategoryMappingConfig().Register(TypeAdapterConfig.GlobalSettings);
    }
}
