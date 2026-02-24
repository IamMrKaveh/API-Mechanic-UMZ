namespace Infrastructure.Security.Configurations;

public sealed class RateLimitEntryConfiguration : IEntityTypeConfiguration<RateLimitEntry>
{
    public void Configure(EntityTypeBuilder<RateLimitEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(256);
    }
}