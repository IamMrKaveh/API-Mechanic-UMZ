using Application.Location.Features.Shared;

namespace Tests.Application.Location.Features.Shared;

public class LocationDtosTests
{
    [Fact]
    public void ProvinceDto_StoresValues()
    {
        var dto = new ProvinceDto(8, "Tehran", "THR");

        dto.Id.ShouldBe(8);
        dto.Name.ShouldBe("Tehran");
        dto.Code.ShouldBe("THR");
    }

    [Fact]
    public void CityDto_StoresValues()
    {
        var dto = new CityDto(15, "Karaj", "Alborz", 5);

        dto.Id.ShouldBe(15);
        dto.Name.ShouldBe("Karaj");
        dto.Province.ShouldBe("Alborz");
        dto.StateId.ShouldBe(5);
    }

    [Fact]
    public void ProvinceDto_ValueEquality_Works()
    {
        new ProvinceDto(1, "A", "A").ShouldBe(new ProvinceDto(1, "A", "A"));
        new ProvinceDto(1, "A", "A").ShouldNotBe(new ProvinceDto(2, "A", "A"));
    }

    [Fact]
    public void CityDto_ValueEquality_Works()
    {
        new CityDto(1, "X", "P", 2).ShouldBe(new CityDto(1, "X", "P", 2));
    }

    [Fact]
    public void Dtos_Deconstruct_Correctly()
    {
        var province = new ProvinceDto(3, "Isfahan", "ISF");
        var (id, name, code) = province;

        id.ShouldBe(3);
        name.ShouldBe("Isfahan");
        code.ShouldBe("ISF");
    }
}
