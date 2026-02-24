namespace Infrastructure.Persistence.Configurations;

public sealed class DiscountUsageConfiguration : IEntityTypeConfiguration<DiscountUsage>
{
    public void Configure(EntityTypeBuilder<DiscountUsage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Order).WithMany(p => p.DiscountUsages).HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => new { e.DiscountCodeId, e.UserId });
        builder.HasIndex(e => new { e.DiscountCodeId, e.OrderId }).IsUnique();
    }
}