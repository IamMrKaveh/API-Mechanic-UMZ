using Domain.Discount.ValueObjects;
using Domain.Order.Aggregates;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Order.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Domain.Order.Aggregates.Order>
{
    public void Configure(EntityTypeBuilder<Domain.Order.Aggregates.Order> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasConversion(v => v.Value, v => OrderId.From(v));

        builder.Property(e => e.UserId)
               .HasConversion(v => v.Value, v => UserId.From(v))
               .IsRequired();

        builder.Property(e => e.OrderNumber)
               .HasConversion(v => v.Value, v => OrderNumber.Create(v))
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.Status)
               .HasConversion(v => v.Value, v => OrderStatusValue.From(v))
               .IsRequired()
               .HasMaxLength(50);

        builder.OwnsOne(e => e.ReceiverInfo, rb =>
        {
            rb.Property(r => r.FullName)
              .HasColumnName("ReceiverFullName")
              .IsRequired()
              .HasMaxLength(150);

            rb.Property(r => r.PhoneNumber)
              .HasColumnName("ReceiverPhoneNumber")
              .IsRequired()
              .HasMaxLength(20);
        });

        builder.OwnsOne(e => e.DeliveryAddress, ab =>
        {
            ab.Property(a => a.Province)
              .HasColumnName("DeliveryProvince")
              .IsRequired().HasMaxLength(100);
            ab.Property(a => a.City)
              .HasColumnName("DeliveryCity")
              .IsRequired().HasMaxLength(100);
            ab.Property(a => a.Street)
              .HasColumnName("DeliveryStreet")
              .IsRequired().HasMaxLength(300);
            ab.Property(a => a.PostalCode)
              .HasColumnName("DeliveryPostalCode")
              .IsRequired().HasMaxLength(20);
        });

        builder.OwnsOne(e => e.SubTotal, mb =>
        {
            mb.Property(m => m.Amount).HasColumnName("SubTotalAmount").HasColumnType("decimal(18,2)");
            mb.Property(m => m.Currency).HasColumnName("SubTotalCurrency").HasMaxLength(5);
        });

        builder.OwnsOne(e => e.ShippingCost, mb =>
        {
            mb.Property(m => m.Amount).HasColumnName("ShippingCostAmount").HasColumnType("decimal(18,2)");
            mb.Property(m => m.Currency).HasColumnName("ShippingCostCurrency").HasMaxLength(5);
        });

        builder.OwnsOne(e => e.DiscountAmount, mb =>
        {
            mb.Property(m => m.Amount).HasColumnName("DiscountAmount").HasColumnType("decimal(18,2)");
            mb.Property(m => m.Currency).HasColumnName("DiscountCurrency").HasMaxLength(5);
        });

        builder.OwnsOne(e => e.FinalAmount, mb =>
        {
            mb.Property(m => m.Amount).HasColumnName("FinalAmount").HasColumnType("decimal(18,2)");
            mb.Property(m => m.Currency).HasColumnName("FinalCurrency").HasMaxLength(5);
        });

        builder.Property(e => e.AppliedDiscountCodeId)
               .HasConversion(
                   v => v == null ? (Guid?)null : v.Value,
                   v => v == null ? null : DiscountCodeId.From(v.Value));

        builder.Property(e => e.IdempotencyKey).IsRequired();
        builder.Property(e => e.CancellationReason).HasMaxLength(500);
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasMany(e => e.OrderItems)
               .WithOne()
               .HasForeignKey("OrderId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}