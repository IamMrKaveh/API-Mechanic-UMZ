using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Configurations;

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => PaymentMethodId.From(value));

        builder.Property(e => e.Name)
            .HasConversion(n => n.Value, v => PaymentMethodName.Create(v))
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Code)
            .HasConversion(c => c.Value, v => PaymentMethodCode.Create(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.IconUrl).HasMaxLength(500);

        builder.OwnsOne(e => e.Fee, fee =>
        {
            fee.OwnsOne(f => f.Amount, m =>
            {
                m.Property(p => p.Amount).HasColumnName("FeeAmount").HasColumnType("decimal(18,2)").IsRequired();
                m.Property(p => p.Currency).HasColumnName("FeeCurrency").HasMaxLength(10).IsRequired();
            });
            fee.Property(f => f.Percentage)
                .HasColumnName("FeePercentage")
                .HasColumnType("decimal(5,2)")
                .IsRequired();
        });

        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.SortOrder);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}