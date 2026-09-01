using Infrastructure.Search.Configurations;

namespace Tests.Infrastructure.Search.Configurations;

public class IndexConfigurationTests
{
    [Fact]
    public void Ctor_WithNoInitialization_UsesDefaultShardsAndReplicas()
    {
        var configuration = new IndexConfiguration();

        configuration.Shards.ShouldBe(1);
        configuration.Replicas.ShouldBe(1);
    }

    [Fact]
    public void Ctor_WithNoInitialization_UsesDefaultMaxResultWindow()
    {
        var configuration = new IndexConfiguration();

        configuration.MaxResultWindow.ShouldBe(10000);
    }

    [Fact]
    public void Ctor_WithNoInitialization_InitializesEmptyAnalyzersDictionary()
    {
        var configuration = new IndexConfiguration();

        configuration.Analyzers.ShouldNotBeNull();
        configuration.Analyzers.ShouldBeEmpty();
    }

    [Fact]
    public void Name_WhenAssigned_ReturnsAssignedValue()
    {
        var configuration = new IndexConfiguration { Name = "products_v1" };

        configuration.Name.ShouldBe("products_v1");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Shards_WhenAssignedPositiveValue_StoresValue(int shards)
    {
        var configuration = new IndexConfiguration { Shards = shards };

        configuration.Shards.ShouldBe(shards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void Replicas_WhenAssignedValue_StoresValue(int replicas)
    {
        var configuration = new IndexConfiguration { Replicas = replicas };

        configuration.Replicas.ShouldBe(replicas);
    }

    [Fact]
    public void MaxResultWindow_WhenAssigned_StoresValue()
    {
        var configuration = new IndexConfiguration { MaxResultWindow = 25_000 };

        configuration.MaxResultWindow.ShouldBe(25_000);
    }

    [Fact]
    public void Analyzers_WhenItemsAdded_AreRetrievableByKey()
    {
        var configuration = new IndexConfiguration();
        var analyzer = new AnalyzerConfiguration
        {
            Type = "custom",
            Tokenizer = "standard",
            Filters = ["lowercase", "asciifolding"]
        };

        configuration.Analyzers["persian_analyzer"] = analyzer;

        configuration.Analyzers.Count.ShouldBe(1);
        configuration.Analyzers["persian_analyzer"].Type.ShouldBe("custom");
        configuration.Analyzers["persian_analyzer"].Tokenizer.ShouldBe("standard");
        configuration.Analyzers["persian_analyzer"].Filters.Count.ShouldBe(2);
    }

    [Fact]
    public void Analyzers_WhenReassignedWithNewDictionary_ReplacesCollection()
    {
        var configuration = new IndexConfiguration();
        configuration.Analyzers["old"] = new AnalyzerConfiguration { Type = "old-type" };

        configuration.Analyzers = new Dictionary<string, AnalyzerConfiguration>
        {
            ["new"] = new AnalyzerConfiguration { Type = "new-type" }
        };

        configuration.Analyzers.Count.ShouldBe(1);
        configuration.Analyzers.ShouldContainKey("new");
        configuration.Analyzers.ShouldNotContainKey("old");
    }

    [Fact]
    public void FullInitialization_WithAllProperties_ExposesEveryValue()
    {
        var configuration = new IndexConfiguration
        {
            Name = "brands_v1",
            Shards = 3,
            Replicas = 2,
            MaxResultWindow = 50_000,
            Analyzers = new Dictionary<string, AnalyzerConfiguration>
            {
                ["default"] = new AnalyzerConfiguration { Type = "custom" }
            }
        };

        configuration.Name.ShouldBe("brands_v1");
        configuration.Shards.ShouldBe(3);
        configuration.Replicas.ShouldBe(2);
        configuration.MaxResultWindow.ShouldBe(50_000);
        configuration.Analyzers.Count.ShouldBe(1);
    }
}
