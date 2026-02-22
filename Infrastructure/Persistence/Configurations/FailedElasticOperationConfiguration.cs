using Application.Search.Features.Shared;

namespace Infrastructure.Persistence.Configurations;

public sealed class FailedElasticOperationConfiguration : IEntityTypeConfiguration<FailedElasticOperation>
{
    public void Configure(EntityTypeBuilder<FailedElasticOperation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Document).IsRequired().HasColumnType("text");
        builder.Property(e => e.Error).IsRequired().HasColumnType("text");
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
    }
}