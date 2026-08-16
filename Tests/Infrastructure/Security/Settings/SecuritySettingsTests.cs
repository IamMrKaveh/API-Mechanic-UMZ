using Infrastructure.Security.Settings;

namespace Tests.Infrastructure.Security.Settings;

public class SecuritySettingsTests
{
    [Fact]
    public void SectionName_HasExpectedConfigurationSection()
    {
        SecuritySettings.SectionName.ShouldBe("Security");
    }

    [Fact]
    public void DefaultInstance_AdminIpWhitelist_IsEmptyList()
    {
        var settings = new SecuritySettings();

        settings.AdminIpWhitelist.ShouldNotBeNull();
        settings.AdminIpWhitelist.ShouldBeEmpty();
    }

    [Fact]
    public void AdminIpWhitelist_WhenValuesAssigned_ReflectsAssignedValues()
    {
        var settings = new SecuritySettings
        {
            AdminIpWhitelist = new List<string> { "127.0.0.1", "10.0.0.1" }
        };

        settings.AdminIpWhitelist.Count.ShouldBe(2);
        settings.AdminIpWhitelist.ShouldContain("127.0.0.1");
        settings.AdminIpWhitelist.ShouldContain("10.0.0.1");
    }
}
