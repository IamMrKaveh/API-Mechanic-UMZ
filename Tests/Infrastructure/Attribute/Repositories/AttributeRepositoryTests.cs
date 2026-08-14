using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Attribute.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Attribute.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AttributeRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private AttributeRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new AttributeRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static Task<AttributeType> BuildTypeAsync(
        string name,
        string displayName,
        int sortOrder = 0,
        bool isActive = true)
    {
        return new AttributeTypeBuilder()
            .WithName(name)
            .WithDisplayName(displayName)
            .WithSortOrder(sortOrder)
            .WithIsActive(isActive)
            .BuildAsync();
    }

    private async Task<AttributeType> PersistTypeAsync(
        string name,
        string displayName,
        int sortOrder = 0,
        bool isActive = true)
    {
        var type = await BuildTypeAsync(name, displayName, sortOrder, isActive);
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();
        return type;
    }

    [RequiresDockerFact]
    public async Task GetAttributeTypeByIdAsync_WhenTypeExists_ReturnsType()
    {
        var persisted = await PersistTypeAsync("color", "Color");

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAttributeTypeByIdAsync(persisted.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(persisted.Id);
        result.Name.ShouldBe("color");
        result.DisplayName.ShouldBe("Color");
    }

    [RequiresDockerFact]
    public async Task GetAttributeTypeByIdAsync_WhenTypeDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetAttributeTypeByIdAsync(AttributeTypeId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetAttributeTypeByIdAsync_WhenTypeIsSoftDeleted_ReturnsNull()
    {
        var persisted = await PersistTypeAsync("size", "Size");

        persisted.MarkAsDeleted(deletedBy: null);
        _context.AttributeTypes.Update(persisted);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAttributeTypeByIdAsync(persisted.Id);

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetAttributeTypeWithValuesAsync_WhenTypeHasValues_IncludesValuesCollection()
    {
        var type = await BuildTypeAsync("color", "Color");
        type.AddValue("red", "Red");
        type.AddValue("blue", "Blue");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAttributeTypeWithValuesAsync(type.Id);

        result.ShouldNotBeNull();
        result.Values.Count.ShouldBe(2);
        result.Values.Select(v => v.Value).ShouldContain("red");
        result.Values.Select(v => v.Value).ShouldContain("blue");
    }

    [RequiresDockerFact]
    public async Task GetAttributeTypeWithValuesAsync_WhenTypeDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetAttributeTypeWithValuesAsync(AttributeTypeId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetAttributeValueByIdAsync_WhenValueExists_ReturnsValueWithAttributeType()
    {
        var type = await BuildTypeAsync("color", "Color");
        var addedValue = type.AddValue("red", "Red", "#FF0000");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAttributeValueByIdAsync(addedValue.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(addedValue.Id);
        result.Value.ShouldBe("red");
        result.DisplayValue.ShouldBe("Red");
        result.HexCode.ShouldBe("#FF0000");
        result.AttributeType.ShouldNotBeNull();
        result.AttributeType.Id.ShouldBe(type.Id);
        result.AttributeType.Name.ShouldBe("color");
    }

    [RequiresDockerFact]
    public async Task GetAttributeValueByIdAsync_WhenValueDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetAttributeValueByIdAsync(AttributeValueId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetAttributeValuesByIdsAsync_WithMatchingIds_ReturnsMatchingValues()
    {
        var type = await BuildTypeAsync("color", "Color");
        var red = type.AddValue("red", "Red");
        var blue = type.AddValue("blue", "Blue");
        var green = type.AddValue("green", "Green");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAttributeValuesByIdsAsync(new[] { red.Id, green.Id });

        var list = result.ToList();
        list.Count.ShouldBe(2);
        list.Select(v => v.Id).ShouldContain(red.Id);
        list.Select(v => v.Id).ShouldContain(green.Id);
        list.Select(v => v.Id).ShouldNotContain(blue.Id);
    }

    [RequiresDockerFact]
    public async Task GetAttributeValuesByIdsAsync_WithEmptyIdList_ReturnsEmpty()
    {
        var type = await BuildTypeAsync("color", "Color");
        type.AddValue("red", "Red");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        var result = await _sut.GetAttributeValuesByIdsAsync(Array.Empty<AttributeValueId>());

        result.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task GetAllAttributeTypesAsync_WithMultipleTypes_ReturnsOrderedBySortOrder()
    {
        await PersistTypeAsync("color", "Color", sortOrder: 30);
        await PersistTypeAsync("size", "Size", sortOrder: 10);
        await PersistTypeAsync("material", "Material", sortOrder: 20);

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAllAttributeTypesAsync();

        result.Count.ShouldBe(3);
        result.Select(t => t.Name).ToList().ShouldBe(new[] { "size", "material", "color" });
    }

    [RequiresDockerFact]
    public async Task GetAllAttributeTypesAsync_WithSoftDeletedType_ExcludesDeletedType()
    {
        var alive = await PersistTypeAsync("color", "Color", sortOrder: 1);
        var deleted = await PersistTypeAsync("size", "Size", sortOrder: 2);

        deleted.MarkAsDeleted(deletedBy: null);
        _context.AttributeTypes.Update(deleted);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAllAttributeTypesAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(alive.Id);
    }

    [RequiresDockerFact]
    public async Task GetAllAttributeTypesAsync_IncludesValuesForEachType()
    {
        var color = await BuildTypeAsync("color", "Color", sortOrder: 1);
        color.AddValue("red", "Red");
        color.AddValue("blue", "Blue");
        _context.AttributeTypes.Add(color);

        var size = await BuildTypeAsync("size", "Size", sortOrder: 2);
        size.AddValue("small", "Small");
        _context.AttributeTypes.Add(size);

        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.GetAllAttributeTypesAsync();

        result.Count.ShouldBe(2);
        result.Single(t => t.Name == "color").Values.Count.ShouldBe(2);
        result.Single(t => t.Name == "size").Values.Count.ShouldBe(1);
    }

    [RequiresDockerFact]
    public async Task AttributeTypeExistsAsync_WhenNameExists_ReturnsTrue()
    {
        await PersistTypeAsync("color", "Color");

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeTypeExistsAsync("color", excludeId: null);

        result.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task AttributeTypeExistsAsync_WhenNameDoesNotExist_ReturnsFalse()
    {
        await PersistTypeAsync("color", "Color");

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeTypeExistsAsync("material", excludeId: null);

        result.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AttributeTypeExistsAsync_WhenNameMatchesExcludedId_ReturnsFalse()
    {
        var persisted = await PersistTypeAsync("color", "Color");

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeTypeExistsAsync("color", excludeId: persisted.Id);

        result.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AttributeValueExistsAsync_WhenValueExistsForType_ReturnsTrue()
    {
        var type = await BuildTypeAsync("color", "Color");
        type.AddValue("red", "Red");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeValueExistsAsync(type.Id, "red", excludeId: null);

        result.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task AttributeValueExistsAsync_WhenValueDoesNotExistForType_ReturnsFalse()
    {
        var type = await BuildTypeAsync("color", "Color");
        type.AddValue("red", "Red");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeValueExistsAsync(type.Id, "green", excludeId: null);

        result.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AttributeValueExistsAsync_WhenValueExistsForDifferentType_ReturnsFalse()
    {
        var color = await BuildTypeAsync("color", "Color");
        color.AddValue("red", "Red");
        _context.AttributeTypes.Add(color);

        var mood = await BuildTypeAsync("mood", "Mood");
        _context.AttributeTypes.Add(mood);

        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeValueExistsAsync(mood.Id, "red", excludeId: null);

        result.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AttributeValueExistsAsync_WhenValueMatchesExcludedId_ReturnsFalse()
    {
        var type = await BuildTypeAsync("color", "Color");
        var red = type.AddValue("red", "Red");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new AttributeRepository(queryContext);

        var result = await sut.AttributeValueExistsAsync(type.Id, "red", excludeId: red.Id);

        result.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AddAttributeTypeAsync_WithValidType_PersistsToDatabase()
    {
        var type = await BuildTypeAsync("color", "Color", sortOrder: 5, isActive: true);

        await _sut.AddAttributeTypeAsync(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.AttributeTypes.FirstOrDefaultAsync(a => a.Id == type.Id);

        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("color");
        persisted.DisplayName.ShouldBe("Color");
        persisted.SortOrder.ShouldBe(5);
        persisted.IsActive.ShouldBeTrue();
        persisted.IsDeleted.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AddAttributeTypeAsync_WithValues_PersistsValuesCascade()
    {
        var type = await BuildTypeAsync("color", "Color");
        type.AddValue("red", "Red", "#FF0000", sortOrder: 1);
        type.AddValue("blue", "Blue", "#0000FF", sortOrder: 2);

        await _sut.AddAttributeTypeAsync(type);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.AttributeTypes
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == type.Id);

        persisted.ShouldNotBeNull();
        persisted.Values.Count.ShouldBe(2);

        var red = persisted.Values.Single(v => v.Value == "red");
        red.DisplayValue.ShouldBe("Red");
        red.HexCode.ShouldBe("#FF0000");
        red.SortOrder.ShouldBe(1);
    }

    [RequiresDockerFact]
    public async Task UpdateAttributeTypeAsync_WithModifiedType_PersistsChanges()
    {
        var persisted = await PersistTypeAsync("color", "Color", sortOrder: 1);

        var checker = Substitute.For<IAttributeTypeUniquenessChecker>();
        checker.IsUniqueAsync(Arg.Any<string>(), Arg.Any<AttributeTypeId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await persisted.Update("shade", "Shade", 42, false, checker);

        await _sut.UpdateAttributeTypeAsync(persisted);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var updated = await queryContext.AttributeTypes.FirstOrDefaultAsync(a => a.Id == persisted.Id);

        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("shade");
        updated.DisplayName.ShouldBe("Shade");
        updated.SortOrder.ShouldBe(42);
        updated.IsActive.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task DeleteAttributeTypeAsync_WithExistingType_MarksAsSoftDeleted()
    {
        var persisted = await PersistTypeAsync("color", "Color");
        var deletedBy = UserId.NewId();

        await _sut.DeleteAttributeTypeAsync(persisted.Id, deletedBy);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();

        var visible = await queryContext.AttributeTypes.FirstOrDefaultAsync(a => a.Id == persisted.Id);
        visible.ShouldBeNull();

        var raw = await queryContext.AttributeTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == persisted.Id);

        raw.ShouldNotBeNull();
        raw.IsDeleted.ShouldBeTrue();
        raw.IsActive.ShouldBeFalse();
        raw.DeletedAt.ShouldNotBeNull();
        raw.DeletedBy.ShouldNotBeNull();
        raw.DeletedBy!.Value.ShouldBe(deletedBy.Value);
    }

    [RequiresDockerFact]
    public async Task DeleteAttributeTypeAsync_WhenTypeNotFound_DoesNothing()
    {
        await Should.NotThrowAsync(async () =>
        {
            await _sut.DeleteAttributeTypeAsync(AttributeTypeId.NewId(), deletedBy: null);
            await _context.SaveChangesAsync();
        });
    }

    [RequiresDockerFact]
    public async Task DeleteAttributeValueAsync_WithExistingValue_DeactivatesValue()
    {
        var type = await BuildTypeAsync("color", "Color");
        var red = type.AddValue("red", "Red");
        _context.AttributeTypes.Add(type);
        await _context.SaveChangesAsync();

        await _sut.DeleteAttributeValueAsync(red.Id, deletedBy: null);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();

        var reloaded = await queryContext.AttributeValues.FirstOrDefaultAsync(v => v.Id == red.Id);
        reloaded.ShouldNotBeNull();
        reloaded.IsActive.ShouldBeFalse();
        reloaded.IsDeleted.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task DeleteAttributeValueAsync_WhenValueNotFound_DoesNothing()
    {
        await Should.NotThrowAsync(async () =>
        {
            await _sut.DeleteAttributeValueAsync(AttributeValueId.NewId(), deletedBy: null);
            await _context.SaveChangesAsync();
        });
    }
}
