namespace Infrastructure.Search.Configurations;

public sealed class ElasticsearchOutboxMessageConfiguration : IEntityTypeConfiguration<ElasticsearchOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ElasticsearchOutboxMessage> builder)
    {
        builder.ToTable("ElasticsearchOutboxMessages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityId).IsRequired();
        builder.Property(e => e.Document).IsRequired();
        builder.Property(e => e.ChangeType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasMaxLength(300).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.RetryCount).IsRequired();
        builder.Property(e => e.ProcessedAt);
        builder.Property(e => e.NextAttemptAt);
        builder.Property(e => e.Error).HasMaxLength(2000);
        builder.Property(e => e.IsPoisoned).HasDefaultValue(false).IsRequired();

        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => new { e.ProcessedAt, e.IsPoisoned, e.NextAttemptAt })
            .HasDatabaseName("IX_ElasticsearchOutboxMessages_Dispatch");
    }
}