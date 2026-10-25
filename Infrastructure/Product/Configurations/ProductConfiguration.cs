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
            .ValueGeneratedNever();

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.OwnsOne(e => e.Name, nb =>
        {
            nb.Property(n => n.Value)
                .HasColumnName("Name")
                .HasMaxLength(ProductName.MaxLength)
                .IsRequired();

            nb.HasIndex(n => n.Value);

            // Trigram index keeps admin/catalog "%term%" searches (ILike) from
            // degrading into sequential scans as the catalog grows. Requires pg_trgm.
            nb.HasIndex(n => n.Value)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Products_Name_trgm");
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
            .IsRequired();

        builder.Property(p => p.CategoryId)
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

        // Composite indexes matching the real admin/catalog filter+sort shapes
        // (every paged list orders by CreatedAt). Standalone boolean indexes
        // (IsActive / IsDeleted) are near-zero selectivity and never chosen.
        builder.HasIndex(p => new { p.IsDeleted, p.CreatedAt })
            .HasDatabaseName("IX_Products_IsDeleted_CreatedAt");

        builder.HasIndex(p => new { p.IsActive, p.IsDeleted, p.CreatedAt })
            .HasDatabaseName("IX_Products_IsActive_IsDeleted_CreatedAt");

        builder.HasIndex(p => new { p.CategoryId, p.CreatedAt })
            .HasDatabaseName("IX_Products_CategoryId_CreatedAt");

        builder.HasIndex(p => new { p.BrandId, p.CreatedAt })
            .HasDatabaseName("IX_Products_BrandId_CreatedAt");
    }
}
