namespace Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Domain.Category.Category>
{
    public void Configure(EntityTypeBuilder<Domain.Category.Category> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name)
            .HasConversion(v => v.Value, v => CategoryName.Create(v))
            .IsRequired()
            .HasMaxLength(CategoryName.MaxLength);

        builder.Property(e => e.Slug)
            .HasColumnName("Slug")
            .HasMaxLength(Slug.MaxLength)
            .IsRequired()
            .HasConversion(v => v != null ? v.Value : Slug.Create(""), v => Slug.FromString(v));

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => e.Slug).IsUnique();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}