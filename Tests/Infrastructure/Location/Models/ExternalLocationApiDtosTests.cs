using System.Reflection;
using System.Text.Json.Serialization;

namespace Tests.Infrastructure.Location.Models;

public class ExternalLocationApiDtosTests
{
    private static Type ProvinceType() =>
        typeof(DBContext).Assembly.GetType(
            "Infrastructure.Location.Models.ExternalProvinceApiDto")
        ?? throw new InvalidOperationException("ExternalProvinceApiDto is not mapped.");

    private static Type CityType() =>
        typeof(DBContext).Assembly.GetType(
            "Infrastructure.Location.Models.ExternalCityApiDto")
        ?? throw new InvalidOperationException("ExternalCityApiDto is not mapped.");

    private static object? Get(object? dto, string property) =>
        dto!.GetType().GetProperty(property)!.GetValue(dto);

    [Fact]
    public void ProvinceDto_DeserializesSnakeCaseJson()
    {
        var dto = JsonSerializer.Deserialize(
            """{"id":5,"name":"تهران","code":"THR"}""", ProvinceType());

        dto.ShouldNotBeNull();
        Get(dto, "Id").ShouldBe(5);
        Get(dto, "Name").ShouldBe("تهران");
        Get(dto, "Code").ShouldBe("THR");
    }

    [Fact]
    public void ProvinceDto_MissingOptionalFields_UseDefaults()
    {
        var dto = JsonSerializer.Deserialize("""{"id":7}""", ProvinceType());

        Get(dto, "Name").ShouldBe(string.Empty);
        Get(dto, "Code").ShouldBeNull();
    }

    [Fact]
    public void ProvinceDto_IgnoresUnknownFields()
    {
        var dto = JsonSerializer.Deserialize(
            """{"id":1,"name":"x","extra":"ignored"}""", ProvinceType());

        Get(dto, "Id").ShouldBe(1);
    }

    [Fact]
    public void ProvinceDto_SerializesWithSnakeCaseNames()
    {
        var dto = Activator.CreateInstance(ProvinceType())!;
        ProvinceType().GetProperty("Id")!.SetValue(dto, 3);
        ProvinceType().GetProperty("Name")!.SetValue(dto, "اصفهان");
        ProvinceType().GetProperty("Code")!.SetValue(dto, "ESF");

        var json = JsonSerializer.Serialize(dto, ProvinceType());
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("id").GetInt32().ShouldBe(3);
        doc.RootElement.GetProperty("name").GetString().ShouldBe("اصفهان");
        doc.RootElement.GetProperty("code").GetString().ShouldBe("ESF");
    }

    [Fact]
    public void CityDto_DeserializesSnakeCaseJson()
    {
        var dto = JsonSerializer.Deserialize(
            """{"id":11,"name":"کرج","province":"البرز","state_id":5}""", CityType());

        dto.ShouldNotBeNull();
        Get(dto, "Id").ShouldBe(11);
        Get(dto, "Name").ShouldBe("کرج");
        Get(dto, "Province").ShouldBe("البرز");
        Get(dto, "StateId").ShouldBe(5);
    }

    [Fact]
    public void CityDto_MissingOptionalProvince_IsNull()
    {
        var dto = JsonSerializer.Deserialize("""{"id":2,"name":"x","state_id":1}""", CityType());

        Get(dto, "Province").ShouldBeNull();
        Get(dto, "StateId").ShouldBe(1);
    }

    [Fact]
    public void CityDto_SerializesStateIdAsSnakeCase()
    {
        var dto = Activator.CreateInstance(CityType())!;
        CityType().GetProperty("StateId")!.SetValue(dto, 9);

        var json = JsonSerializer.Serialize(dto, CityType());
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("state_id").GetInt32().ShouldBe(9);
    }

    [Theory]
    [InlineData("ExternalProvinceApiDto", "Id", "id")]
    [InlineData("ExternalProvinceApiDto", "Name", "name")]
    [InlineData("ExternalProvinceApiDto", "Code", "code")]
    [InlineData("ExternalCityApiDto", "Id", "id")]
    [InlineData("ExternalCityApiDto", "Name", "name")]
    [InlineData("ExternalCityApiDto", "Province", "province")]
    [InlineData("ExternalCityApiDto", "StateId", "state_id")]
    public void Dtos_ExposeExpectedJsonPropertyNames(string typeName, string property, string jsonName)
    {
        var type = typeof(DBContext).Assembly.GetType($"Infrastructure.Location.Models.{typeName}")!;
        var attribute = type.GetProperty(property)!
            .GetCustomAttribute<JsonPropertyNameAttribute>();

        attribute.ShouldNotBeNull();
        attribute!.Name.ShouldBe(jsonName);
    }
}
