using Domain.Brand.ValueObjects;
using Domain.Product.ValueObjects;

namespace Infrastructure.Product.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Domain.Product.Aggregates.Product>
{
    public void Configure(EntityTypeBuilder<Domain.Product.Aggregates.Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => ProductId.From(v))
            .ValueGeneratedNever();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.OwnsOne(e => e.Name, nb =>
        {
            nb.Property(n => n.Value)
                .HasColumnName("Name")
                .HasMaxLength(ProductName.MaxLength)
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

        builder.Property(e => e.Description)
            .HasColumnType("text");

        builder.Property(e => e.BrandId)
            .HasConversion(v => v.Value, v => BrandId.From(v))
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.IsFeatured)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .IsRequired();

        builder.Property(e => e.DeletedAt);

        builder.Property(e => e.DeletedBy);

        builder.HasOne(e => e.Brand)
            .WithMany()
            .HasForeignKey(e => e.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.BrandId);

        builder.HasIndex(e => e.IsActive);

        builder.HasIndex(e => e.IsDeleted);
    }
}