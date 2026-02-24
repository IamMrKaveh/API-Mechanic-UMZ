namespace Infrastructure.Persistence.Configurations;

public sealed class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .HasConversion(
                v => v.Value,
                s => DiscountCodeValue.FromPersistedString(s))
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.Property(e => e.Percentage).HasColumnType("decimal(5,2)");
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.HasMany(e => e.Restrictions)
               .WithOne(e => e.DiscountCode)
               .HasForeignKey(e => e.DiscountCodeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}