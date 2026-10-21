using System.Reflection;
using FluentValidation;
using ICommandContract = Application.Common.Interfaces.ICommand;

namespace Tests.Architecture;

/// <summary>
/// Every command that carries input must have a validator, so that a malformed
/// request is rejected in the MediatR pipeline before it reaches the domain.
///
/// Commands without any input have nothing to validate and are exempt.
/// </summary>
public class CommandValidatorCoverageTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(ICommandContract).Assembly;

    [Fact]
    public void EveryCommandWithInput_HasAValidator()
    {
        var missing = new List<string>();

        foreach (var commandType in GetCommandTypes())
        {
            if (!HasInput(commandType))
                continue;

            var validatorType = commandType.Assembly
                .GetTypes()
                .FirstOrDefault(t =>
                    t is { IsClass: true, IsAbstract: false }
                    && t.BaseType == typeof(AbstractValidator<>).MakeGenericType(commandType));

            if (validatorType is null)
                missing.Add(commandType.FullName ?? commandType.Name);
        }

        missing.ShouldBeEmpty(
            "Every command with input needs a validator; the missing ones are listed above.");
    }

    [Fact]
    public void EveryValidatorIsRegisteredWithMediatR()
    {
        // AddValidatorsFromAssembly registers every IValidator<T> in the Application
        // assembly, so an open generic or non-public validator would be silently
        // skipped. This test pins the convention.
        var problems = new List<string>();

        foreach (var commandType in GetCommandTypes())
        {
            if (!HasInput(commandType))
                continue;

            foreach (var validatorType in ApplicationAssembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false }
                    && t.BaseType == typeof(AbstractValidator<>).MakeGenericType(commandType)))
            {
                if (!validatorType.IsPublic)
                    problems.Add($"{validatorType.Name} is not public");
            }
        }

        problems.ShouldBeEmpty();
    }

    private static IEnumerable<Type> GetCommandTypes()
        => ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal))
            .Where(t => typeof(ICommandContract).IsAssignableFrom(t));

    private static bool HasInput(Type commandType)
    {
        var properties = commandType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => !p.Name.StartsWith("Audit", StringComparison.Ordinal))
            .ToList();

        return properties.Count > 0;
    }
}

