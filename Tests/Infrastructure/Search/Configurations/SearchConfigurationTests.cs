using Infrastructure.Search.Configurations;

namespace Tests.Infrastructure.Search.Configurations;

public class SearchConfigurationTests
{
    [Fact]
    public void Ctor_WithNoInitialization_UsesDefaultPageSizes()
    {
        var configuration = new SearchConfiguration();

        configuration.DefaultPageSize.ShouldBe(20);
        configuration.MaxPageSize.ShouldBe(100);
    }

    [Fact]
    public void Ctor_WithNoInitialization_EnablesFuzzySearchByDefault()
    {
        var configuration = new SearchConfiguration();

        configuration.EnableFuzzySearch.ShouldBeTrue();
    }

    [Fact]
    public void Ctor_WithNoInitialization_EnablesHighlightingByDefault()
    {
        var configuration = new SearchConfiguration();

        configuration.EnableHighlighting.ShouldBeTrue();
    }

    [Fact]
    public void Ctor_WithNoInitialization_EnablesSuggestionsByDefault()
    {
        var configuration = new SearchConfiguration();

        configuration.EnableSuggestions.ShouldBeTrue();
    }

    [Fact]
    public void Ctor_WithNoInitialization_UsesDefaultSuggestionCount()
    {
        var configuration = new SearchConfiguration();

        configuration.SuggestionCount.ShouldBe(5);
    }

    [Fact]
    public void Ctor_WithNoInitialization_LeavesMinScoreNull()
    {
        var configuration = new SearchConfiguration();

        configuration.MinScore.ShouldBeNull();
    }

    [Fact]
    public void Ctor_WithNoInitialization_PopulatesDefaultFieldBoosts()
    {
        var configuration = new SearchConfiguration();

        configuration.FieldBoosts.ShouldNotBeNull();
        configuration.FieldBoosts.Count.ShouldBe(4);
        configuration.FieldBoosts["name"].ShouldBe(5.0);
        configuration.FieldBoosts["categoryName"].ShouldBe(3.0);
        configuration.FieldBoosts["brandName"].ShouldBe(2.0);
        configuration.FieldBoosts["description"].ShouldBe(1.0);
    }

    [Fact]
    public void DefaultFieldBoosts_OrderedByWeight_NameHasHighestPriority()
    {
        var configuration = new SearchConfiguration();

        configuration.FieldBoosts["name"]
            .ShouldBeGreaterThan(configuration.FieldBoosts["categoryName"]);
        configuration.FieldBoosts["categoryName"]
            .ShouldBeGreaterThan(configuration.FieldBoosts["brandName"]);
        configuration.FieldBoosts["brandName"]
            .ShouldBeGreaterThan(configuration.FieldBoosts["description"]);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void DefaultPageSize_WhenAssigned_StoresValue(int pageSize)
    {
        var configuration = new SearchConfiguration { DefaultPageSize = pageSize };

        configuration.DefaultPageSize.ShouldBe(pageSize);
    }

    [Fact]
    public void MaxPageSize_WhenAssigned_StoresValue()
    {
        var configuration = new SearchConfiguration { MaxPageSize = 500 };

        configuration.MaxPageSize.ShouldBe(500);
    }

    [Fact]
    public void MinScore_WhenAssigned_StoresValue()
    {
        var configuration = new SearchConfiguration { MinScore = 0.75 };

        configuration.MinScore.ShouldBe(0.75);
    }

    [Fact]
    public void EnableFuzzySearch_WhenDisabled_ReflectsAssignedValue()
    {
        var configuration = new SearchConfiguration { EnableFuzzySearch = false };

        configuration.EnableFuzzySearch.ShouldBeFalse();
    }

    [Fact]
    public void EnableHighlighting_WhenDisabled_ReflectsAssignedValue()
    {
        var configuration = new SearchConfiguration { EnableHighlighting = false };

        configuration.EnableHighlighting.ShouldBeFalse();
    }

    [Fact]
    public void EnableSuggestions_WhenDisabled_ReflectsAssignedValue()
    {
        var configuration = new SearchConfiguration { EnableSuggestions = false };

        configuration.EnableSuggestions.ShouldBeFalse();
    }

    [Fact]
    public void SuggestionCount_WhenAssigned_StoresValue()
    {
        var configuration = new SearchConfiguration { SuggestionCount = 15 };

        configuration.SuggestionCount.ShouldBe(15);
    }

    [Fact]
    public void FieldBoosts_WhenExtendedWithAdditionalField_ContainsBothDefaultAndCustomFields()
    {
        var configuration = new SearchConfiguration();

        configuration.FieldBoosts["sku"] = 4.5;

        configuration.FieldBoosts.Count.ShouldBe(5);
        configuration.FieldBoosts["sku"].ShouldBe(4.5);
        configuration.FieldBoosts.ShouldContainKey("name");
    }

    [Fact]
    public void FieldBoosts_WhenReplacedWithNewDictionary_UsesReplacementCollection()
    {
        var configuration = new SearchConfiguration
        {
            FieldBoosts = new Dictionary<string, double>
            {
                ["title"] = 10.0
            }
        };

        configuration.FieldBoosts.Count.ShouldBe(1);
        configuration.FieldBoosts.ShouldContainKey("title");
        configuration.FieldBoosts.ShouldNotContainKey("name");
    }
}
