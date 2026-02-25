namespace Infrastructure.Order.Configurations;

public class OrderProcessStateConfiguration : IEntityTypeConfiguration<OrderProcessState>
{
    public void Configure(EntityTypeBuilder<OrderProcessState> builder)
    {
        builder.ToTable("OrderProcessStates");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderId)
            .IsRequired();

        builder.Property(s => s.CurrentStep)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.FailureReason)
            .HasMaxLength(500);

        builder.Property(s => s.CorrelationId)
            .HasMaxLength(100);

        builder.HasIndex(s => s.OrderId)
            .IsUnique();
    }
}