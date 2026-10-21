using System.Reflection;
using SharedKernel.Abstractions;
using AggregateRootBase = Domain.Common.Abstractions.AggregateRoot<object>;
using ApplicationCommand = Application.Common.Interfaces.ICommand;
using DbContextType = Infrastructure.Persistence.Context.DBContext;
using BaseApiController = Presentation.Base.Endpoints.v1.BaseApiController;
using ActivitySources = SharedContracts.Diagnostics.ApplicationActivitySources;

namespace Tests.Architecture;

/// <summary>
/// Enforces the layering rules of the solution at the assembly level.
///
///   Presentation  -> Application, SharedKernel, SharedContracts
///   Application   -> Domain, SharedKernel, SharedContracts
///   Infrastructure-> Application, Domain, SharedKernel, SharedContracts
///   Domain        -> SharedKernel, SharedContracts
///   SharedKernel  -> (no project references)
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRootBase).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DbContextType).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(BaseApiController).Assembly;
    private static readonly Assembly SharedKernelAssembly = typeof(ValueObject).Assembly;
    private static readonly Assembly SharedContractsAssembly = typeof(ActivitySources).Assembly;

    private static readonly string[] AllowedReferences =
    [
        "Domain",
        "Application",
        "Infrastructure",
        "Presentation",
        "SharedKernel",
        "SharedContracts",
        "System",
        "Microsoft",
        "MediatR",
        "FluentValidation",
        "Mapster",
        "NSubstitute",
        "xunit",
        "Shouldly",
        "Bogus",
        "Microsoft.Extensions",
        "Microsoft.AspNetCore",
        "Npgsql",
        "Microsoft.EntityFrameworkCore",
        "Swashbuckle",
        "Scrutor",
        "Polly",
        "Elastic",
        "StackExchange.Redis",
        "AWSSDK",
        "BCrypt",
        "Testcontainers",
        "Respawn",
        "Google",
        "Kavenegar",
        "Serilog",
        "OpenTelemetry",
        "NexGen",
        "AutoMapper",
        "Dapper",
        "System.Text",
        "netstandard",
        "mscorlib",
        "WindowsBase"
    ];

    [Fact]
    public void Domain_DoesNotDependOnApplicationInfrastructureOrPresentation()
    {
        AssertNoForbiddenReferences(DomainAssembly, "Domain");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrPresentation()
    {
        AssertNoForbiddenReferences(ApplicationAssembly, "Application");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnPresentation()
    {
        var forbidden = InfrastructureAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name.Contains("Presentation", StringComparison.Ordinal))
            .ToList();

        forbidden.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Domain", "Application")]
    [InlineData("Domain", "Infrastructure")]
    [InlineData("Domain", "Presentation")]
    [InlineData("Application", "Infrastructure")]
    [InlineData("Application", "Presentation")]
    [InlineData("Infrastructure", "Presentation")]
    [InlineData("SharedKernel", "Domain")]
    [InlineData("SharedKernel", "Application")]
    [InlineData("SharedKernel", "Infrastructure")]
    [InlineData("SharedKernel", "Presentation")]
    [InlineData("SharedContracts", "Domain")]
    [InlineData("SharedContracts", "Application")]
    [InlineData("SharedContracts", "Infrastructure")]
    [InlineData("SharedContracts", "Presentation")]
    public void Layer_DoesNotReferenceALayerAboveIt(string layer, string forbiddenLayer)
    {
        var assembly = ResolveAssembly(layer);

        var references = assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        references.ShouldNotContain(
            forbiddenLayer,
            $"'{layer}' must not reference '{forbiddenLayer}'.");
    }

    private static Assembly ResolveAssembly(string layer) => layer switch
    {
        "Domain" => DomainAssembly,
        "Application" => ApplicationAssembly,
        "Infrastructure" => InfrastructureAssembly,
        "Presentation" => PresentationAssembly,
        "SharedKernel" => SharedKernelAssembly,
        "SharedContracts" => SharedContractsAssembly,
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown layer.")
    };

    [Fact]
    public void SharedKernel_DoesNotDependOnAnyProject()
    {
        var forbidden = SharedKernelAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name is "Domain" or "Application" or "Infrastructure" or "Presentation")
            .ToList();

        forbidden.ShouldBeEmpty();
    }

    [Fact]
    public void SharedContracts_DoesNotDependOnAnyProject()
    {
        var forbidden = SharedContractsAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name is "Domain" or "Application" or "Infrastructure" or "Presentation")
            .ToList();

        forbidden.ShouldBeEmpty();
    }

    private static void AssertNoForbiddenReferences(Assembly assembly, string layerName)
    {
        var forbidden = assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name =>
                (layerName != "Domain" && name == "Application")
                || (layerName != "Application" && layerName != "Domain" && name == "Infrastructure")
                || (name == "Presentation"))
            .ToList();

        forbidden.ShouldBeEmpty(
            $"'{layerName}' must not reference {string.Join(", ", forbidden)}.");
    }
}
