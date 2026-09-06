using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class MiddlewareExtensionsTests
{
    [Fact]
    public void UseApplication_WiresPipelineAndReturnsSameApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Swagger:Enabled"] = "false"
        });
        builder.Services.Configure<RequestLocalizationOptions>(_ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication();
        builder.Services.AddRateLimiter(_ => { });
        builder.Services.AddHealthChecks();
        using var app = builder.Build();

        var result = app.UseApplication();

        result.ShouldBeSameAs(app);
    }
}
