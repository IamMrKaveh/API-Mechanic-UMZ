using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;
using Presentation.Common.Filters;
using Presentation.Common.Interfaces;

namespace Tests.Presentation.Common.Extensions;

public class ControllersExtensionsTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPresentationControllers();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddPresentationControllers_RegistersRateLimitAndValidationFilters()
    {
        var mvc = BuildProvider().GetRequiredService<IOptions<MvcOptions>>().Value;

        mvc.Filters.OfType<ServiceFilterAttribute>()
            .Any(f => f.ServiceType == typeof(OtpRateLimitFilter)).ShouldBeTrue();
        mvc.Filters.OfType<ServiceFilterAttribute>()
            .Any(f => f.ServiceType == typeof(ReviewRateLimitFilter)).ShouldBeTrue();
        mvc.Filters.OfType<TypeFilterAttribute>()
            .Any(f => f.ImplementationType == typeof(ValidationFilter)).ShouldBeTrue();
    }

    [Fact]
    public void AddPresentationControllers_ConfiguresJsonOptions()
    {
        var json = BuildProvider()
            .GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        json.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        json.DictionaryKeyPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        json.PropertyNameCaseInsensitive.ShouldBeTrue();
        json.DefaultIgnoreCondition.ShouldBe(JsonIgnoreCondition.WhenWritingNull);
        json.Converters.OfType<JsonStringEnumConverter>().Count().ShouldBe(1);
    }

    [Fact]
    public void AddPresentationControllers_SuppressesModelStateInvalidFilter()
    {
        var behavior = BuildProvider()
            .GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

        behavior.SuppressModelStateInvalidFilter.ShouldBeTrue();
    }

    [Fact]
    public void AddPresentationControllers_RegistersHttpResultMapperAndContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPresentationControllers();

        services.ShouldContain(s =>
            s.ServiceType == typeof(IHttpResultMapper) &&
            s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s => s.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddPresentationControllers_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddPresentationControllers().ShouldBeSameAs(services);
    }
}
