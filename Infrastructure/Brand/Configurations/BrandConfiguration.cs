using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Infrastructure.Brand.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Domain.Brand.Aggregates.Brand>
{
    public void Configure(EntityTypeBuilder<Domain.Brand.Aggregates.Brand> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => BrandId.From(v));

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.OwnsOne(e => e.Name, nb =>
        {
            nb.Property(p => p.Value)
                .HasColumnName("Name")
                .HasMaxLength(BrandName.MaxLength)
                .IsRequired();
            nb.HasIndex(p => p.Value);
        });

        builder.OwnsOne(e => e.Slug, sb =>
        {
            sb.Property(p => p.Value)
                .HasColumnName("Slug")
                .HasMaxLength(Slug.MaxLength)
                .IsRequired();
            sb.HasIndex(p => p.Value).IsUnique();
        });

        builder.Property(e => e.CategoryId)
            .HasConversion(v => v.Value, v => CategoryId.From(v))
            .IsRequired();

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.LogoPath).HasMaxLength(1000);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => new { e.CategoryId });

        builder.Navigation(e => e.Name).IsRequired();
        builder.Navigation(e => e.Slug).IsRequired();
    }
}