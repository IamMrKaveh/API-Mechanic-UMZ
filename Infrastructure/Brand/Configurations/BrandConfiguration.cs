using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Brand.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Domain.Brand.Aggregates.Brand>
{
    public void Configure(EntityTypeBuilder<Domain.Brand.Aggregates.Brand> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => BrandId.From(v));

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.Name)
            .HasConversion(v => v.Value, v => BrandName.Create(v))
            .IsRequired()
            .HasMaxLength(BrandName.MaxLength);

        builder.Property(e => e.Slug)
            .HasConversion(v => v != null ? v.Value : string.Empty, v => Slug.FromString(v))
            .HasColumnName("Slug")
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .HasConversion(v => v.Value, v => CategoryId.From(v))
            .IsRequired();

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.LogoPath).HasMaxLength(1000);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => new { e.CategoryId, e.Name });
    }
}