using Application.Common.Options;

namespace Tests.Application.Common.Options;

public class ApiBaseUrlOptionsTests
{
    [Fact]
    public void SectionName_IsApi()
    {
        ApiBaseUrlOptions.SectionName.ShouldBe("Api");
    }

    [Fact]
    public void PublicBaseUrl_HasDefaultLocalhostValue()
    {
        var sut = new ApiBaseUrlOptions();

        sut.PublicBaseUrl.ShouldBe("https://localhost:44318");
    }

    [Fact]
    public void PublicBaseUrl_CanBeOverridden()
    {
        var sut = new ApiBaseUrlOptions
        {
            PublicBaseUrl = "https://api.example.com"
        };

        sut.PublicBaseUrl.ShouldBe("https://api.example.com");
    }
}
