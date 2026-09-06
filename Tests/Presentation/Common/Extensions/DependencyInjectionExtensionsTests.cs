using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class DependencyInjectionExtensionsTests
{
    [Fact]
    public void AddCustomApiVersioning_ConfiguresDefaultVersionAndReaders()
    {
        var services = new ServiceCollection();
        services.AddCustomApiVersioning();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        options.DefaultApiVersion.ShouldBe(new ApiVersion(1, 0));
        options.AssumeDefaultVersionWhenUnspecified.ShouldBeTrue();
        options.ReportApiVersions.ShouldBeTrue();
        options.ApiVersionReader.ShouldBeOfType<UrlSegmentApiVersionReader>();
    }

    [Fact]
    public void AddCustomApiVersioning_ConfiguresApiExplorer()
    {
        var services = new ServiceCollection();
        services.AddCustomApiVersioning();

        var provider = services.BuildServiceProvider();
        var explorer = provider.GetRequiredService<IOptions<ApiExplorerOptions>>().Value;

        explorer.GroupNameFormat.ShouldBe("'v'VVV");
        explorer.SubstituteApiVersionInUrl.ShouldBeTrue();
    }

    [Fact]
    public void AddCustomApiVersioning_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddCustomApiVersioning().ShouldBeSameAs(services);
    }
}
