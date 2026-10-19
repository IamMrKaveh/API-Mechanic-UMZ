namespace Tests.TestInfrastructure.Database;

[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
}
