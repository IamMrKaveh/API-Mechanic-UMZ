using Infrastructure.DataProtection.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class DataProtectionExtensions
{
    public static IDataProtectionBuilder PersistKeysToSingletonRedisRepository(
    this IDataProtectionBuilder builder,
    IConnectionMultiplexer redis,
    string keyPrefix)
    {
        builder.Services.AddSingleton<IXmlRepository>(provider =>
        new ResilientRedisXmlRepository(
        redis,
        provider.GetRequiredService<ILogger<ResilientRedisXmlRepository>>(),
        keyPrefix,
        TimeSpan.FromDays(90)));
        return builder;
    }
}