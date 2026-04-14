using Domain.Order.ValueObjects;

namespace Infrastructure.Audit.Configurations;

public sealed class OrderProcessStateConfiguration : IEntityTypeConfiguration<OrderProcessState>
{
    public void Configure(EntityTypeBuilder<OrderProcessState> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderId)
            .HasConversion(v => v.Value, v => OrderId.From(v))
            .IsRequired();

        builder.Property(e => e.CurrentStep).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.CurrentStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.FailureReason).HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);

        builder.HasIndex(e => e.OrderId).IsUnique();
    }
}