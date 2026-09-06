using Microsoft.OpenApi.Models;
using Presentation.Common.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tests.Presentation.Common.Swagger;

public class NullableSchemaFilterTests
{
    private sealed class SampleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public int Age { get; set; }
        public int? Score { get; set; }
        public List<string>? Tags { get; set; }
    }

    private sealed class AllRequiredDto
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private static SchemaFilterContext BuildContext(Type type) =>
        new(type, Substitute.For<ISchemaGenerator>(), new SchemaRepository());

    private static OpenApiSchema BuildSchema(params string[] required) =>
        new()
        {
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new(),
                ["nickname"] = new(),
                ["age"] = new(),
                ["score"] = new(),
                ["tags"] = new()
            },
            Required = new SortedSet<string>(required, StringComparer.OrdinalIgnoreCase)
        };

    [Fact]
    public void Apply_NullableProperties_BecomeNullableAndOptional()
    {
        var schema = BuildSchema("name", "nickname", "age", "score", "tags");

        new NullableSchemaFilter().Apply(schema, BuildContext(typeof(SampleDto)));

        schema.Properties["nickname"].Nullable.ShouldBeTrue();
        schema.Properties["score"].Nullable.ShouldBeTrue();
        schema.Properties["tags"].Nullable.ShouldBeTrue();
        schema.Required.ShouldNotContain("nickname");
        schema.Required.ShouldNotContain("score");
        schema.Required.ShouldNotContain("tags");
    }

    [Fact]
    public void Apply_NonNullableProperties_StayUntouched()
    {
        var schema = BuildSchema("name", "age");

        new NullableSchemaFilter().Apply(schema, BuildContext(typeof(SampleDto)));

        schema.Properties["name"].Nullable.ShouldBeFalse();
        schema.Properties["age"].Nullable.ShouldBeFalse();
        schema.Required.ShouldContain("name");
        schema.Required.ShouldContain("age");
    }

    [Fact]
    public void Apply_TypeWithoutNullableProperties_DoesNothing()
    {
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new(),
                ["age"] = new()
            },
            Required = new SortedSet<string>(["name", "age"], StringComparer.OrdinalIgnoreCase)
        };

        new NullableSchemaFilter().Apply(schema, BuildContext(typeof(AllRequiredDto)));

        schema.Properties["name"].Nullable.ShouldBeFalse();
        schema.Required.ShouldBe(["name", "age"], ignoreOrder: true);
    }

    [Fact]
    public void Apply_SchemaWithoutProperties_DoesNothing()
    {
        var schema = new OpenApiSchema();

        Should.NotThrow(() => new NullableSchemaFilter().Apply(schema, BuildContext(typeof(SampleDto))));
    }
}
