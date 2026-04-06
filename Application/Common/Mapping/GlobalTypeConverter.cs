using Mapster;

namespace Application.Common.Mapping;

public class GlobalTypeConverter : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Guid, string>().MapWith(src => src.ToString());
        config.NewConfig<string, Guid>().MapWith(src => Guid.TryParse(src, out var g) ? g : Guid.Empty);
        config.NewConfig<DateTime, string>().MapWith(src => src.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}