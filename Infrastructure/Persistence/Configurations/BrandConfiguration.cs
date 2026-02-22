namespace Infrastructure.Persistence.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasConversion(v => v.Value, v => Domain.Brand.ValueObjects.BrandName.Create(v))
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Slug)
            .HasColumnName("Slug")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(v => v.Value, v => Domain.Category.ValueObjects.Slug.FromString(v));

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.SortOrder).HasDefaultValue(0);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(d => d.Category)
               .WithMany(p => p.Brands)
               .HasForeignKey(d => d.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}