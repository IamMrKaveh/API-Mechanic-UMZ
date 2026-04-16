using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Search.Configurations;

public sealed class ElasticsearchOutboxMessageConfiguration : IEntityTypeConfiguration<ElasticsearchOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ElasticsearchOutboxMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityId).IsRequired();
        builder.Property(e => e.Document).HasColumnType("text").IsRequired();
        builder.Property(e => e.ChangeType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.RetryCount).IsRequired();
        builder.Property(e => e.Error).HasMaxLength(2000);

        builder.HasIndex(e => e.ProcessedAt);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        builder.ToTable("ElasticsearchOutboxMessages");
    }
}