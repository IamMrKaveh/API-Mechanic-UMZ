using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Order.Configurations;

public sealed class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasConversion(v => v.Value, v => OrderStatusId.From(v));

        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Icon).HasMaxLength(100);
        builder.Property(e => e.Color).HasMaxLength(50);
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDefault).IsRequired();
        builder.Property(e => e.AllowCancel).IsRequired();
        builder.Property(e => e.AllowEdit).IsRequired();

        builder.HasIndex(e => e.Name).IsUnique();
    }
}