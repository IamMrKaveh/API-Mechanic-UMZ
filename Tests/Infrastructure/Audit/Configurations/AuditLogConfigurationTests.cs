using Domain.Audit.Entities;

namespace Tests.Infrastructure.Audit.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AuditLogConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task SaveChanges_PersistsAllScalarPropertiesAndRoundTripsAuditLogId()
    {
        var auditLog = new AuditLogBuilder()
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("10.0.0.1")
            .WithEntityType("User")
            .WithEntityId("entity-42")
            .WithDetails("Successful login")
            .WithUserAgent("Mozilla/5.0")
            .Build();
        auditLog.ClearDomainEvents();

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLog.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(auditLog.Id);
        reloaded.EventType.ShouldBe("Security");
        reloaded.Action.ShouldBe("Login");
        reloaded.IpAddress.ShouldBe("10.0.0.1");
        reloaded.EntityType.ShouldBe("User");
        reloaded.EntityId.ShouldBe("entity-42");
        reloaded.Details.ShouldBe("Successful login");
        reloaded.UserAgent.ShouldBe("Mozilla/5.0");
        reloaded.IntegrityHash.ShouldNotBeNullOrWhiteSpace();
        reloaded.HashVersion.ShouldBe(AuditLog.CurrentHashVersion);
        reloaded.IsArchived.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_WithNullUserId_PersistsUserIdAsNull()
    {
        var auditLog = new AuditLogBuilder()
            .WithUserId(null)
            .WithEventType("System")
            .WithAction("Startup")
            .WithIpAddress("127.0.0.1")
            .Build();
        auditLog.ClearDomainEvents();

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AuditLogs.FirstAsync(a => a.Id == auditLog.Id);

        reloaded.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_WithUserId_RoundTripsStronglyTypedUserId()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var auditLog = new AuditLogBuilder()
            .WithUserId(user.Id)
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("10.0.0.2")
            .Build();
        auditLog.ClearDomainEvents();

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AuditLogs.FirstAsync(a => a.Id == auditLog.Id);

        reloaded.UserId.ShouldNotBeNull();
        reloaded.UserId!.Value.ShouldBe(user.Id.Value);
    }

    [Fact]
    public void Model_EventType_IsRequiredAndHasMaxLength100()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.EventType));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_Action_IsRequiredAndHasMaxLength200()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.Action));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(200);
    }

    [Fact]
    public void Model_IpAddress_IsRequiredAndHasMaxLength45()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.IpAddress));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(45);
    }

    [Fact]
    public void Model_UserAgent_IsNullableAndHasMaxLength500()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.UserAgent));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void Model_EntityType_IsNullableAndHasMaxLength100()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.EntityType));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_EntityId_IsNullableAndHasMaxLength100()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.EntityId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_IntegrityHash_IsRequiredAndHasMaxLength200()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.IntegrityHash));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(200);
    }

    [Fact]
    public void Model_Details_HasTextColumnType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.Details));
        property.ShouldNotBeNull();
        property!.GetColumnType().ShouldBe("text");
    }

    [Fact]
    public void Model_HashVersion_IsRequiredAndHasDefaultValueOne()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.HashVersion));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetDefaultValue().ShouldBe(1);
    }

    [Fact]
    public void Model_CreatedAt_UsesTimestampWithTimeZoneColumnType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AuditLog.CreatedAt));
        property.ShouldNotBeNull();
        property!.GetColumnType().ShouldBe("timestamp(6) with time zone");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(AuditLog.Id));
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(AuditLog.UserId));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnCreatedAt()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(AuditLog.CreatedAt));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnEventType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(AuditLog.EventType));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnHashVersion()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(AuditLog.HashVersion));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasNamedIndexOnEntityType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_AuditLogs_EntityType");

        index.ShouldNotBeNull();
        index!.Properties.Count.ShouldBe(1);
        index.Properties[0].Name.ShouldBe(nameof(AuditLog.EntityType));
    }

    [Fact]
    public void Model_HasNamedIndexOnIsArchived()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_AuditLogs_IsArchived");

        index.ShouldNotBeNull();
        index!.Properties.Count.ShouldBe(1);
        index.Properties[0].Name.ShouldBe(nameof(AuditLog.IsArchived));
    }

    [Fact]
    public void Model_HasCompositeIndexOnCreatedAtIsArchivedEventType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_AuditLogs_CreatedAt_IsArchived_EventType");

        index.ShouldNotBeNull();
        index!.Properties.Count.ShouldBe(3);
        index.Properties.Any(p => p.Name == nameof(AuditLog.CreatedAt)).ShouldBeTrue();
        index.Properties.Any(p => p.Name == nameof(AuditLog.IsArchived)).ShouldBeTrue();
        index.Properties.Any(p => p.Name == nameof(AuditLog.EventType)).ShouldBeTrue();
    }

    [Fact]
    public void Model_HasCompositeIndexOnEntityTypeEntityId()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditLog));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_AuditLogs_EntityType_EntityId");

        index.ShouldNotBeNull();
        index!.Properties.Count.ShouldBe(2);
        index.Properties.Any(p => p.Name == nameof(AuditLog.EntityType)).ShouldBeTrue();
        index.Properties.Any(p => p.Name == nameof(AuditLog.EntityId)).ShouldBeTrue();
    }
}
