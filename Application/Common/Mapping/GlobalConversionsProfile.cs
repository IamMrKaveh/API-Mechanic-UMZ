namespace Application.Common.Mapping;

public sealed class GlobalConversionsProfile : Profile
{
    public GlobalConversionsProfile()
    {
        CreateMap<byte[]?, string?>()
            .ConvertUsing(src => src == null ? null : Convert.ToBase64String(src));
    }
}