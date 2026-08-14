using Application.Common.Contracts;
using Infrastructure.Common.Services;
using Microsoft.Extensions.Configuration;

namespace Tests.Infrastructure.Common.Services;

public class UrlResolverServiceTests
{
    [Fact]
    public void ImplementsIUrlResolverService()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>()));

        sut.ShouldBeAssignableTo<IUrlResolverService>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveMediaUrl_WithNullOrWhitespaceFilePath_ReturnsEmptyString(string? filePath)
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://cdn.example.com"
        }));

        sut.ResolveMediaUrl(filePath!).ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("http://cdn.example.com/image.jpg")]
    [InlineData("https://cdn.example.com/image.jpg")]
    [InlineData("HTTPS://cdn.example.com/image.jpg")]
    [InlineData("HtTp://cdn.example.com/image.jpg")]
    public void ResolveMediaUrl_WhenFilePathAlreadyAbsoluteHttpUrl_ReturnsFilePathUnchanged(string filePath)
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://other.example.com"
        }));

        sut.ResolveMediaUrl(filePath).ShouldBe(filePath);
    }

    [Fact]
    public void ResolveMediaUrl_WithStorageBaseUrlAndRelativePath_ProducesCombinedUrl()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://cdn.example.com"
        }));

        sut.ResolveMediaUrl("images/photo.jpg").ShouldBe("https://cdn.example.com/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenStorageBaseUrlHasTrailingSlash_TrimsSingleTrailingSlash()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://cdn.example.com/"
        }));

        sut.ResolveMediaUrl("images/photo.jpg").ShouldBe("https://cdn.example.com/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenFilePathHasLeadingSlash_TrimsLeadingSlash()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://cdn.example.com"
        }));

        sut.ResolveMediaUrl("/images/photo.jpg").ShouldBe("https://cdn.example.com/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenBothStorageAndPathHaveSeparators_ProducesSingleSlashSeparator()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://cdn.example.com/"
        }));

        sut.ResolveMediaUrl("/images/photo.jpg").ShouldBe("https://cdn.example.com/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenStorageBaseUrlMissing_FallsBackToLiaraBaseUrl()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Liara:BaseUrl"] = "https://liara.example.com"
        }));

        sut.ResolveMediaUrl("images/photo.jpg").ShouldBe("https://liara.example.com/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenStorageBaseUrlPresent_PrefersItOverLiaraBaseUrl()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:BaseUrl"] = "https://cdn.example.com",
            ["Liara:BaseUrl"] = "https://liara.example.com"
        }));

        sut.ResolveMediaUrl("images/photo.jpg").ShouldBe("https://cdn.example.com/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenNeitherStorageNorLiaraBaseUrlProvided_ReturnsPathWithLeadingSlashAndEmptyBase()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>()));

        sut.ResolveMediaUrl("images/photo.jpg").ShouldBe("/images/photo.jpg");
    }

    [Fact]
    public void ResolveMediaUrl_WhenNeitherBaseUrlProvidedAndPathHasLeadingSlash_StillReturnsSingleLeadingSlash()
    {
        var sut = new UrlResolverService(BuildConfiguration(new Dictionary<string, string?>()));

        sut.ResolveMediaUrl("/images/photo.jpg").ShouldBe("/images/photo.jpg");
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> data) =>
        new ConfigurationBuilder().AddInMemoryCollection(data).Build();
}
