namespace Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Domain.Product.Product>
{
    public void Configure(EntityTypeBuilder<Domain.Product.Product> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name)
            .HasConversion(v => v.Value, v => Domain.Product.ValueObjects.ProductName.Create(v))
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(d => d.Brand)
            .WithMany(p => p.Products)
            .HasForeignKey(d => d.BrandId);

        builder.OwnsOne(e => e.Stats, s =>
        {
            s.Property(p => p.MinPrice).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
            s.Property(p => p.MaxPrice).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
            s.Property(p => p.TotalStock);
            s.Property(p => p.AverageRating).HasColumnType("decimal(3,2)");
            s.Property(p => p.ReviewCount);
            s.Property(p => p.SalesCount);
        });

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}