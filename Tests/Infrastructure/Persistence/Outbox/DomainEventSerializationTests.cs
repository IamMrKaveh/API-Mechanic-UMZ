using System.Reflection;
using System.Text.Json;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Outbox;
using SharedKernel.ValueObjects;
using DomainEventContract = Domain.Common.Abstractions.IDomainEvent;
using ValueObjectBase = SharedKernel.Abstractions.ValueObject;

namespace Tests.Infrastructure.Persistence.Outbox;

public class DomainEventSerializationTests
{
    [Fact]
    public void OrderExpiredEvent_SurvivesRoundTrip()
    {
        var orderId = OrderId.NewId();
        var original = new OrderExpiredEvent(orderId);

        var restored = RoundTrip<OrderExpiredEvent>(original);

        restored.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void PaymentSucceededEvent_SurvivesRoundTrip()
    {
        var original = new PaymentSucceededEvent(
            PaymentTransactionId.NewId(),
            OrderId.NewId(),
            12345L,
            UserId.NewId(),
            Money.Create(50_000m, "IRT"),
            PaymentGateway.Zarinpal);

        var restored = RoundTrip<PaymentSucceededEvent>(original);

        restored.OrderId.ShouldBe(original.OrderId);
        restored.UserId.ShouldBe(original.UserId);
        restored.Amount.ShouldBe(original.Amount);
        restored.Gateway.ShouldBe(original.Gateway);
    }

    [Fact]
    public void DomainEvent_KeepsItsIdentityAndTimingAcrossRoundTrip()
    {
        var original = new OrderExpiredEvent(OrderId.NewId());

        var restored = RoundTrip<OrderExpiredEvent>(original);

        restored.EventId.ShouldBe(original.EventId);
        restored.OccurredAt.ShouldBe(original.OccurredAt);
        restored.CorrelationId.ShouldBe(original.CorrelationId);
    }

    [Fact]
    public void EveryDomainEventType_CanBeSerializedAndRebuilt()
    {
        var failures = new List<string>();

        foreach (var type in GetDomainEventTypes())
        {
            var instance = TryCreateInstance(type, failures);

            if (instance is null)
                continue;

            try
            {
                var payload = JsonSerializer.Serialize(instance, type, DomainEventJson.Options);
                var restored = JsonSerializer.Deserialize(payload, type, DomainEventJson.Options);

                if (restored is null)
                    failures.Add($"{type.Name}: deserialized to null");
            }
            catch (Exception ex)
            {
                failures.Add($"{type.Name}: {ex.GetType().Name} {ex.Message}");
            }
        }

        failures.ShouldBeEmpty();
    }

    private static T RoundTrip<T>(T original)
    {
        var payload = JsonSerializer.Serialize(original, original!.GetType(), DomainEventJson.Options);
        var restored = JsonSerializer.Deserialize(payload, original.GetType(), DomainEventJson.Options);

        restored.ShouldNotBeNull();

        return (T)restored!;
    }

    private static IEnumerable<Type> GetDomainEventTypes()
        => typeof(DomainEventContract).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(DomainEventContract).IsAssignableFrom(type))
            .OrderBy(type => type.FullName);

    private static bool IsSupportedParameter(Type type)
    {
        if (type == typeof(string)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(decimal)
            || type == typeof(bool)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTime?)
            || type == typeof(int?)
            || type == typeof(long?)
            || type == typeof(decimal?)
            || type == typeof(bool?)
            || type.IsEnum)
        {
            return true;
        }

        if (typeof(IStronglyTypedId).IsAssignableFrom(type))
            return FindFromMethod(type) is not null;

        if (typeof(ValueObjectBase).IsAssignableFrom(type))
            return true;

        return FindSingleArgumentFactory(type) is not null;
    }

    private static MethodInfo? FindFromMethod(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var method = current.GetMethod(
                "From",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Guid)],
                modifiers: null);

            if (method is not null)
                return method;
        }

        return null;
    }

    private static MethodInfo? FindSingleArgumentFactory(Type type)
        => type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name is "Create" or "FromString" or "From"
                && m.GetParameters().Length == 1);
    private static object? TryCreateInstance(Type type, List<string> failures)
    {
        var constructor = type.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().All(p => p.HasDefaultValue || IsSupportedParameter(p.ParameterType)));

        if (constructor is null)
        {
            failures.Add($"{type.Name}: no usable constructor");
            return null;
        }

        var parameters = constructor.GetParameters();

        foreach (var arguments in BuildArgumentCombinations(parameters))
        {
            try
            {
                return constructor.Invoke(arguments);
            }
            catch (Exception ex)
            {
                lastError = ex.InnerException?.Message ?? ex.Message;
            }
        }

        failures.Add($"{type.Name}: {lastError}");
        return null;
    }

    private static string lastError = string.Empty;

    private static IEnumerable<object?[]> BuildArgumentCombinations(ParameterInfo[] parameters)
    {
        var candidates = parameters
            .Select(p => CandidateValuesFor(p.ParameterType))
            .ToArray();

        var combinations = new List<object?[]>();

        void Build(int index, object?[] current)
        {
            if (index == candidates.Length)
            {
                combinations.Add([.. current]);
                return;
            }

            foreach (var candidate in candidates[index])
            {
                current[index] = candidate;
                Build(index + 1, current);
            }
        }

        Build(0, new object?[parameters.Length]);

        return combinations;
    }

    private static IEnumerable<object?> CandidateValuesFor(Type type)
    {
        if (type == typeof(string))
            return ["test", "0123456789abcdef", "TEST-VALUE-123456", "ORD-20260101-ABCDEFGH", "a@b.com"];

        if (type == typeof(int))
            return [3, 1];

        if (type == typeof(long))
            return [1L, 2L];

        if (type == typeof(decimal))
            return [10m, 1m];

        if (type == typeof(bool))
            return [true];

        if (type == typeof(Guid))
            return [Guid.NewGuid()];

        if (type == typeof(DateTime))
            return [new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)];

        if (type == typeof(DateTime?))
            return [new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)];

        if (type == typeof(int?))
            return [1];

        if (type == typeof(long?))
            return [1L];

        if (type == typeof(decimal?))
            return [10m];

        if (type == typeof(bool?))
            return [true];

        if (type.IsEnum)
            return [Enum.GetValues(type).GetValue(0)];

        if (typeof(IStronglyTypedId).IsAssignableFrom(type))
        {
            var from = FindFromMethod(type);
            return from is null ? [null] : [from.Invoke(null, [Guid.NewGuid()])];
        }

        var factory = FindSingleArgumentFactory(type);

        if (factory is not null)
        {
            var parameterType = factory.GetParameters()[0].ParameterType;
            var rawValues = parameterType == typeof(int) ? new object?[] { 3 } : ["test", "0123456789abcdef"];

            var results = new List<object?>();

            foreach (var raw in rawValues)
            {
                try
                {
                    results.Add(factory.Invoke(null, [raw]));
                }
                catch
                {
                    // try the next candidate
                }
            }

            return results.Count > 0 ? results : [null];
        }

        return [null];
    }
}
