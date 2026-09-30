using System.Reflection;
using Application.Cache.Contracts;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Attributes;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Shouldly;

namespace Tests.Infrastructure.Cache.OutputCache;

/// <summary>
/// Guards every <c>[RequestOutputCache]</c> request in the Application layer against the mistakes
/// that would only show up in production: unknown tags (never evicted), double caching with the
/// legacy <c>ICacheableQuery</c>, and responses that the Redis provider cannot read back.
/// </summary>
public class OutputCacheContractTests
{
    private static readonly Assembly ApplicationAssembly = typeof(CacheTags).Assembly;

    private static IEnumerable<Type> CachedRequests() => ApplicationAssembly
        .GetTypes()
        .Where(t => t.GetCustomAttribute<RequestOutputCacheAttribute>() is not null)
        .OrderBy(t => t.FullName, StringComparer.Ordinal);

    public static TheoryData<string> CachedRequestNames()
    {
        var data = new TheoryData<string>();
        foreach (var type in CachedRequests())
            data.Add(type.FullName!);
        return data;
    }

    private static Type Resolve(string fullName) => ApplicationAssembly.GetType(fullName, throwOnError: true)!;

    private static DBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql("Host=localhost;Database=mechanic_model_tests;Username=test;Password=test")
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));
    }

    [Fact]
    public void ApplicationLayer_HasCachedRequests()
    {
        CachedRequests().Count().ShouldBeGreaterThan(30);
    }

    [Theory]
    [MemberData(nameof(CachedRequestNames))]
    public void CachedRequest_UsesExplicitTagsAndTtl(string requestName)
    {
        var attribute = Resolve(requestName).GetCustomAttribute<RequestOutputCacheAttribute>()!;

        attribute.Tags.ShouldNotBeEmpty();
        attribute.ExpirationInSeconds.ShouldBeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(CachedRequestNames))]
    public void CachedRequest_TagsAreEfEntityTypeNames(string requestName)
    {
        using var context = CreateContext();
        var entityNames = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToHashSet(StringComparer.Ordinal);

        var tags = Resolve(requestName).GetCustomAttribute<RequestOutputCacheAttribute>()!.Tags;

        foreach (var tag in tags)
        {
            var known = entityNames.Contains(tag) || CacheTags.Manual.All.Contains(tag);
            known.ShouldBeTrue($"tag '{tag}' on {requestName} is neither an EF entity name nor a manual tag");
        }
    }

    [Theory]
    [InlineData(typeof(global::Application.Analytics.Features.Queries.GetDashboardStatistics.GetDashboardStatisticsQuery), 600)]
    [InlineData(typeof(global::Application.Analytics.Features.Queries.GetSalesChartData.GetSalesChartDataQuery), 900)]
    [InlineData(typeof(global::Application.Attribute.Features.Queries.GetAllAttributeTypes.GetAllAttributeTypesQuery), 3600)]
    [InlineData(typeof(global::Application.Brand.Features.Queries.GetPublicBrands.GetPublicBrandsQuery), 1800)]
    [InlineData(typeof(global::Application.Category.Features.Queries.GetCategoryTree.GetCategoryTreeQuery), 3600)]
    [InlineData(typeof(global::Application.Inventory.Features.Queries.GetAllWarehouses.GetAllWarehousesQuery), 3600)]
    [InlineData(typeof(global::Application.Location.Features.Queries.GetCities.GetCitiesQuery), 86400)]
    [InlineData(typeof(global::Application.Location.Features.Queries.GetStates.GetStatesQuery), 86400)]
    [InlineData(typeof(global::Application.Order.Features.Queries.GetOrderStatuses.GetOrderStatusesQuery), 600)]
    [InlineData(typeof(global::Application.Payment.Features.Queries.GetPaymentMethods.GetPaymentMethodsQuery), 1800)]
    [InlineData(typeof(global::Application.Product.Features.Queries.GetProduct.GetProductQuery), 600)]
    [InlineData(typeof(global::Application.Shipping.Features.Queries.GetShippings.GetShippingsQuery), 1800)]
    public void MigratedQuery_KeepsItsPreviousTtl(Type queryType, int expectedSeconds)
    {
        queryType.GetCustomAttribute<RequestOutputCacheAttribute>()!
            .ExpirationInSeconds.ShouldBe(expectedSeconds);
    }

    [Fact]
    public void ReportingAndStaticReferenceQueries_UseManualTagsOnly()
    {
        foreach (var type in new[]
        {
            typeof(global::Application.Analytics.Features.Queries.GetRevenueReport.GetRevenueReportQuery),
            typeof(global::Application.Location.Features.Queries.GetStates.GetStatesQuery)
        })
        {
            var tags = type.GetCustomAttribute<RequestOutputCacheAttribute>()!.Tags;
            tags.ShouldAllBe(t => CacheTags.Manual.All.Contains(t));
        }
    }

    [Theory]
    [MemberData(nameof(CachedRequestNames))]
    public void CachedRequest_ResponseSurvivesRedisJsonRoundTrip(string requestName)
    {
        var responseType = Resolve(requestName)
            .GetInterfaces()
            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GetGenericArguments()[0];

        responseType.IsGenericType.ShouldBeTrue();
        responseType.GetGenericTypeDefinition().ShouldBe(typeof(ServiceResult<>));
        var payloadType = responseType.GetGenericArguments()[0];

        var payload = SamplePayload.Build(payloadType)!;
        var json = JsonConvert.SerializeObject(payload);
        var restored = JsonConvert.DeserializeObject(json, payloadType);

        JsonConvert.SerializeObject(restored).ShouldBe(json,
            $"{payloadType.Name} loses data through Newtonsoft (used by the Redis provider)");
    }

    /// <summary>Builds a fully populated instance of a DTO graph using only public members.</summary>
    private static class SamplePayload
    {
        public static object? Build(Type type, int depth = 0)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string) || type == typeof(object)) return "x";
            if (type == typeof(bool)) return true;
            if (type == typeof(Guid)) return Guid.Parse("11111111-2222-3333-4444-555555555555");
            if (type == typeof(DateTime)) return new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
            if (type == typeof(DateOnly)) return new DateOnly(2024, 1, 2);
            if (type == typeof(TimeOnly)) return new TimeOnly(3, 4, 5);
            if (type == typeof(TimeSpan)) return TimeSpan.FromMinutes(5);
            if (type.IsEnum) return Enum.GetValues(type).GetValue(Enum.GetValues(type).Length > 1 ? 1 : 0);
            if (type.IsPrimitive || type == typeof(decimal)) return Convert.ChangeType(3, type);
            if (depth > 6) return null;

            if (type.IsArray)
            {
                var element = type.GetElementType()!;
                var array = Array.CreateInstance(element, 1);
                array.SetValue(Build(element, depth + 1), 0);
                return array;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var args = type.GetGenericArguments();

                if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) ||
                    definition == typeof(IReadOnlyDictionary<,>))
                {
                    var dictionary = (System.Collections.IDictionary)Activator.CreateInstance(
                        typeof(Dictionary<,>).MakeGenericType(args))!;
                    dictionary.Add(Build(args[0], depth + 1)!, Build(args[1], depth + 1));
                    return dictionary;
                }

                if (definition == typeof(List<>) || definition == typeof(IList<>) ||
                    definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>) ||
                    definition == typeof(ICollection<>) || definition == typeof(IReadOnlyCollection<>))
                {
                    var list = (System.Collections.IList)Activator.CreateInstance(
                        typeof(List<>).MakeGenericType(args[0]))!;
                    list.Add(Build(args[0], depth + 1));
                    return list;
                }
            }

            if (type.IsAbstract || type.IsInterface) return null;

            var constructor = type.GetConstructor(Type.EmptyTypes)
                ?? type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (constructor is null) return null;

            var instance = constructor.Invoke(
                constructor.GetParameters().Select(p => Build(p.ParameterType, depth + 1)).ToArray());

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.SetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0)
                    property.SetValue(instance, Build(property.PropertyType, depth + 1));
            }

            return instance;
        }
    }
}
