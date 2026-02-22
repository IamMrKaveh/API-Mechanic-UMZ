namespace Infrastructure.Persistence.Configurations;

public sealed class DiscountRestrictionConfiguration : IEntityTypeConfiguration<DiscountRestriction>
{
    public void Configure(EntityTypeBuilder<DiscountRestriction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(e => new { e.DiscountCodeId, e.Type, e.EntityId }).IsUnique();
    }
}