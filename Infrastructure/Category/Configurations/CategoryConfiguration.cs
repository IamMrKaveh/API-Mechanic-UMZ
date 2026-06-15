using Domain.Category.ValueObjects;

namespace Infrastructure.Category.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Domain.Category.Aggregates.Category>
{
    public void Configure(EntityTypeBuilder<Domain.Category.Aggregates.Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => CategoryId.From(v))
            .ValueGeneratedNever();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.OwnsOne(e => e.Name, nb =>
        {
            nb.Property(n => n.Value)
                .HasColumnName("Name")
                .HasMaxLength(CategoryName.MaxLength)
                .IsRequired();

            nb.HasIndex(n => n.Value)
                .IsUnique();
        });

        builder.OwnsOne(e => e.Slug, sb =>
        {
            sb.Property(s => s.Value)
                .HasColumnName("Slug")
                .HasMaxLength(Slug.MaxLength)
                .IsRequired();

            sb.HasIndex(s => s.Value)
                .IsUnique();
        });

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.SortOrder)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Ignore(e => e.Brands);
    }
}