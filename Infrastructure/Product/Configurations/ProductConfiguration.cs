using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
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

        builder.Property(p => p.BrandId)
            .HasConversion(id => id.Value, value => BrandId.From(value))
            .IsRequired();

        builder.Property(p => p.CategoryId)
            .HasConversion(id => id.Value, value => CategoryId.From(value))
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

        builder.HasOne(p => p.Brand)
            .WithMany()
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.BrandId);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsDeleted);
    }
}