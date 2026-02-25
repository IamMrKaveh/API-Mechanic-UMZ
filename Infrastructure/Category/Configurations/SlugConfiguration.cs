namespace Infrastructure.Category.Configurations;

public sealed class SlugConfiguration : IEntityTypeConfiguration<Slug>
{
    public void Configure(EntityTypeBuilder<Slug> builder)
    {
        builder
            .HasNoKey()
            .ToView(null);
    }
}