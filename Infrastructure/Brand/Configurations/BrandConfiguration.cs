using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Infrastructure.Brand.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Domain.Brand.Aggregates.Brand>
{
    public void Configure(EntityTypeBuilder<Domain.Brand.Aggregates.Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => BrandId.From(v))
            .ValueGeneratedNever();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.OwnsOne(e => e.Name, nb =>
        {
            nb.Property(n => n.Value)
                .HasColumnName("Name")
                .HasMaxLength(BrandName.MaxLength)
                .IsRequired();

            nb.HasIndex(n => n.Value);
        });

        builder.OwnsOne(e => e.Slug, sb =>
        {
            sb.Property(s => s.Value)
                .HasColumnName("Slug")
                .HasMaxLength(Slug.MaxLength)
                .IsRequired();

            sb.HasIndex(s => s.Value)
                .IsUnique();
        });

        builder.Property(e => e.CategoryId)
            .HasConversion(v => v.Value, v => CategoryId.From(v))
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.LogoPath)
            .HasMaxLength(1000);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.CategoryId);

        builder.Ignore(e => e.Products);
    }
}