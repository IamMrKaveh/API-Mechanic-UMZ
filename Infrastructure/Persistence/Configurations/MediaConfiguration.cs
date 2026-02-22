namespace Infrastructure.Persistence.Configurations;

public sealed class MediaConfiguration : IEntityTypeConfiguration<Domain.Media.Media>
{
    public void Configure(EntityTypeBuilder<Domain.Media.Media> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.FileType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.AltText).HasMaxLength(500);

        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}