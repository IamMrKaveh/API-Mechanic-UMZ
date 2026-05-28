using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Presentation.Common.Options;

namespace Presentation.Common.Extensions;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddTrustedForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var proxySettings = configuration
            .GetSection(ProxySettings.SectionName)
            .Get<ProxySettings>() ?? new ProxySettings();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();

            foreach (var proxy in proxySettings.TrustedProxies)
            {
                if (IPAddress.TryParse(proxy, out var ip))
                    options.KnownProxies.Add(ip);
            }

            foreach (var network in proxySettings.TrustedNetworks)
            {
                var parts = network.Split('/');
                if (parts.Length == 2 &&
                    IPAddress.TryParse(parts[0], out var prefix) &&
                    int.TryParse(parts[1], out var length))
                {
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length));
                }
            }
        });

        return services;
    }
}