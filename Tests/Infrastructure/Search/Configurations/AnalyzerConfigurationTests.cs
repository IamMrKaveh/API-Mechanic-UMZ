using Infrastructure.Search.Configurations;

namespace Tests.Infrastructure.Search.Configurations;

public class AnalyzerConfigurationTests
{
    [Fact]
    public void Ctor_WithNoInitialization_LeavesTokenizerAsNullAndFiltersEmpty()
    {
        var configuration = new AnalyzerConfiguration();

        configuration.Tokenizer.ShouldBeNull();
        configuration.Filters.ShouldNotBeNull();
        configuration.Filters.ShouldBeEmpty();
    }

    [Fact]
    public void Type_WhenAssigned_ReturnsAssignedValue()
    {
        var configuration = new AnalyzerConfiguration { Type = "custom" };

        configuration.Type.ShouldBe("custom");
    }

    [Fact]
    public void Tokenizer_WhenAssigned_ReturnsAssignedValue()
    {
        var configuration = new AnalyzerConfiguration { Tokenizer = "standard" };

        configuration.Tokenizer.ShouldBe("standard");
    }

    [Fact]
    public void Filters_WhenItemsAdded_PreservesInsertionOrder()
    {
        var configuration = new AnalyzerConfiguration();

        configuration.Filters.Add("lowercase");
        configuration.Filters.Add("asciifolding");
        configuration.Filters.Add("stop");

        configuration.Filters.Count.ShouldBe(3);
        configuration.Filters[0].ShouldBe("lowercase");
        configuration.Filters[1].ShouldBe("asciifolding");
        configuration.Filters[2].ShouldBe("stop");
    }

    [Fact]
    public void Filters_WhenReassignedWithNewList_ReplacesEntireCollection()
    {
        var configuration = new AnalyzerConfiguration();
        configuration.Filters.Add("lowercase");

        configuration.Filters = ["stop", "asciifolding"];

        configuration.Filters.Count.ShouldBe(2);
        configuration.Filters.ShouldContain("stop");
        configuration.Filters.ShouldContain("asciifolding");
        configuration.Filters.ShouldNotContain("lowercase");
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("custom")]
    [InlineData("keyword")]
    [InlineData("persian")]
    public void Type_WhenSetToVariousValues_StoresExactString(string type)
    {
        var configuration = new AnalyzerConfiguration { Type = type };

        configuration.Type.ShouldBe(type);
    }

    [Fact]
    public void FullInitialization_WithAllProperties_ExposesEveryValue()
    {
        var configuration = new AnalyzerConfiguration
        {
            Type = "custom",
            Tokenizer = "standard",
            Filters = ["lowercase", "asciifolding"]
        };

        configuration.Type.ShouldBe("custom");
        configuration.Tokenizer.ShouldBe("standard");
        configuration.Filters.Count.ShouldBe(2);
    }
}
