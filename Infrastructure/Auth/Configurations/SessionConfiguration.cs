using Domain.Security.Aggregates;

namespace Infrastructure.Auth.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property(e => e.Selector).IsRequired().HasMaxLength(100);
        builder.Property(e => e.HashedVerifier).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.RevocationReason).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Selector).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.IsRevoked, e.ExpiresAt });
    }
}