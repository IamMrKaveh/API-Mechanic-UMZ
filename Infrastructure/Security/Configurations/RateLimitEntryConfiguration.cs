namespace Infrastructure.Security.Configurations;

public sealed class RateLimitEntryConfiguration : IEntityTypeConfiguration<RateLimitEntry>
{
    public void Configure(EntityTypeBuilder<RateLimitEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(300);
        builder.Property(e => e.WindowKey).IsRequired().HasMaxLength(100);
        builder.HasIndex(e => new { e.Key, e.WindowKey }).IsUnique();
    }
}