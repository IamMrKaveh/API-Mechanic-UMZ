using Application.Search.Features.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Search.Configurations;

public sealed class FailedElasticOperationConfiguration : IEntityTypeConfiguration<FailedElasticOperation>
{
    public void Configure(EntityTypeBuilder<FailedElasticOperation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Document).HasColumnType("text").IsRequired();
        builder.Property(e => e.Error).HasColumnType("text").IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RetryCount).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        builder.ToTable("FailedElasticOperations");
    }
}