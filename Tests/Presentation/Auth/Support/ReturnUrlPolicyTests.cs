using Presentation.Auth.Support;

namespace Tests.Presentation.Auth.Support;

public class ReturnUrlPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_WithMissingValue_ReturnsRoot(string? value)
    {
        ReturnUrlPolicy.Sanitize(value).ShouldBe("/");
    }

    [Fact]
    public void Sanitize_WithSiteRelativePath_KeepsIt()
    {
        ReturnUrlPolicy.Sanitize("/dashboard").ShouldBe("/dashboard");
    }

    [Fact]
    public void Sanitize_WithProtocolRelativeUrl_ReturnsRoot()
    {
        ReturnUrlPolicy.Sanitize("//evil.example").ShouldBe("/");
    }

    [Fact]
    public void Sanitize_WithAbsoluteUrl_ReturnsRoot()
    {
        ReturnUrlPolicy.Sanitize("https://evil.example").ShouldBe("/");
    }

    [Fact]
    public void Sanitize_WithBackslashTrick_ReturnsRoot()
    {
        ReturnUrlPolicy.Sanitize("/\\evil").ShouldBe("/");
    }

    [Fact]
    public void Sanitize_WithJavascriptScheme_ReturnsRoot()
    {
        ReturnUrlPolicy.Sanitize("javascript:alert(1)").ShouldBe("/");
    }

    [Theory]
    [InlineData("/dashboard/orders")]
    [InlineData("/dashboard?tab=orders")]
    [InlineData("/")]
    public void Sanitize_WithSafePaths_KeepsThem(string value)
    {
        ReturnUrlPolicy.Sanitize(value).ShouldBe(value);
    }

    [Fact]
    public void Sanitize_WithControlCharacter_ReturnsRoot()
    {
        ReturnUrlPolicy.Sanitize("/dash\nboard").ShouldBe("/");
    }
}
