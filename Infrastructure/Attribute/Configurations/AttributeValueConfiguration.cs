namespace Infrastructure.Attribute.Configurations;

public sealed class AttributeValueConfiguration : IEntityTypeConfiguration<AttributeValue>
{
    public void Configure(EntityTypeBuilder<AttributeValue> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Value).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DisplayValue).IsRequired().HasMaxLength(100);
        builder.Property(e => e.HexCode).HasMaxLength(50);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}