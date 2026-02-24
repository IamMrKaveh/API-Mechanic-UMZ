namespace Infrastructure.Order.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Domain.Order.Order>
{
    public void Configure(EntityTypeBuilder<Domain.Order.Order> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AddressSnapshot)
               .HasConversion(v => v.ToJson(), v => AddressSnapshot.FromJson(v))
               .IsRequired()
               .HasColumnType("jsonb");

        builder.Property(e => e.Status)
               .HasConversion(v => v.Value, v => OrderStatusValue.FromString(v))
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.OrderNumber)
               .HasConversion(v => v.Value, v => OrderNumber.FromString(v))
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.ReceiverName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(256);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => e.OrderNumber).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.OrderItems)
               .WithOne(e => e.Order)
               .HasForeignKey(e => e.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Shipping)
               .WithMany()
               .HasForeignKey(d => d.ShippingId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}