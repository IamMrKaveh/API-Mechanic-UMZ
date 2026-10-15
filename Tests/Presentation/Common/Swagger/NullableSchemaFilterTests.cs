using Microsoft.OpenApi;
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
        new(type, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), type, null);

    private static OpenApiSchema BuildSchema(params string[] required) =>
        new()
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["name"] = new OpenApiSchema(),
                ["nickname"] = new OpenApiSchema(),
                ["age"] = new OpenApiSchema(),
                ["score"] = new OpenApiSchema(),
                ["tags"] = new OpenApiSchema()
            },
            Required = new SortedSet<string>(required, StringComparer.OrdinalIgnoreCase)
        };

    [Fact]
    public void Apply_NullableProperties_BecomeNullableAndOptional()
    {
        var schema = BuildSchema("name", "nickname", "age", "score", "tags");

        new NullableSchemaFilter().Apply(schema, BuildContext(typeof(SampleDto)));

        schema.Properties!["nickname"].Type.ShouldBe(JsonSchemaType.Null);
        schema.Properties!["score"].Type.ShouldBe(JsonSchemaType.Null);
        schema.Properties!["tags"].Type.ShouldBe(JsonSchemaType.Null);
        schema.Required!.ShouldNotContain("nickname");
        schema.Required!.ShouldNotContain("score");
        schema.Required!.ShouldNotContain("tags");
    }

    [Fact]
    public void Apply_NonNullableProperties_StayUntouched()
    {
        var schema = BuildSchema("name", "age");

        new NullableSchemaFilter().Apply(schema, BuildContext(typeof(SampleDto)));

        schema.Properties!["name"].Type.ShouldNotBe(JsonSchemaType.Null);
        schema.Properties!["age"].Type.ShouldNotBe(JsonSchemaType.Null);
        schema.Required!.ShouldContain("name");
        schema.Required!.ShouldContain("age");
    }

    [Fact]
    public void Apply_TypeWithoutNullableProperties_DoesNothing()
    {
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["name"] = new OpenApiSchema(),
                ["age"] = new OpenApiSchema()
            },
            Required = new SortedSet<string>(["name", "age"], StringComparer.OrdinalIgnoreCase)
        };

        new NullableSchemaFilter().Apply(schema, BuildContext(typeof(AllRequiredDto)));

        schema.Properties!["name"].Type.ShouldNotBe(JsonSchemaType.Null);
        schema.Required!.ShouldBe(["name", "age"], ignoreOrder: true);
    }

    [Fact]
    public void Apply_SchemaWithoutProperties_DoesNothing()
    {
        var schema = new OpenApiSchema();

        Should.NotThrow(() => new NullableSchemaFilter().Apply(schema, BuildContext(typeof(SampleDto))));
    }
}
