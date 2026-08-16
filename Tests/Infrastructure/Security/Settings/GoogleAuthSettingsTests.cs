using Infrastructure.Security.Settings;

namespace Tests.Infrastructure.Security.Settings;

public class GoogleAuthSettingsTests
{
    [Fact]
    public void SectionName_HasExpectedConfigurationSection()
    {
        GoogleAuthSettings.SectionName.ShouldBe("Authentication:Google");
    }

    [Fact]
    public void DefaultInstance_ClientIdAndClientSecret_AreEmptyStrings()
    {
        var settings = new GoogleAuthSettings();

        settings.ClientId.ShouldBe(string.Empty);
        settings.ClientSecret.ShouldBe(string.Empty);
    }

    [Fact]
    public void Properties_WhenValuesAssigned_ReflectAssignedValues()
    {
        var settings = new GoogleAuthSettings
        {
            ClientId = "client-id-value",
            ClientSecret = "client-secret-value"
        };

        settings.ClientId.ShouldBe("client-id-value");
        settings.ClientSecret.ShouldBe("client-secret-value");
    }
}
