using Domain.Brand.ValueObjects;
using Domain.Product.Aggregates;
using Domain.Product.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Product.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Domain.Product.Aggregates.Product>
{
    public void Configure(EntityTypeBuilder<Domain.Product.Aggregates.Product> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => ProductId.From(v));

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasColumnType("text");

        builder.Property(e => e.BrandId)
            .HasConversion(v => v.Value, v => BrandId.From(v))
            .IsRequired();

        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsFeatured).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne(e => e.Brand)
            .WithMany()
            .HasForeignKey(e => e.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ProductVariants)
            .WithOne()
            .HasForeignKey("ProductId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.BrandId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.IsDeleted);

        builder.ToTable("Products");
    }
}