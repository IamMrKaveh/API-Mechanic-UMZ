namespace Infrastructure.Persistence.Configurations;

public sealed class ElasticsearchOutboxMessageConfiguration : IEntityTypeConfiguration<ElasticsearchOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ElasticsearchOutboxMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).HasMaxLength(100);
        builder.Property(e => e.EntityId).HasMaxLength(100);
        builder.Property(e => e.OperationType).HasMaxLength(50);
        builder.Property(e => e.Payload).HasColumnType("text");
        builder.Property(e => e.Document).HasColumnType("text");
        builder.Property(e => e.ChangeType).HasMaxLength(50);
        builder.Property(e => e.Error).HasColumnType("text");
    }
}