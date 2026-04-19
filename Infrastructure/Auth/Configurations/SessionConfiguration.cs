using Domain.Security.Aggregates;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => SessionId.From(v));

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(e => e.RefreshToken)
            .HasConversion(v => v.Value, v => RefreshToken.Create(v))
            .HasColumnName("RefreshToken")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.DeviceInfo)
            .HasConversion(v => v.Value, v => DeviceInfo.Create(v))
            .HasColumnName("DeviceInfo")
            .HasMaxLength(500);

        builder.Property(e => e.IpAddress)
            .HasConversion(v => v.Value, v => IpAddress.Create(v))
            .HasColumnName("IpAddress")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(e => e.IsRevoked).IsRequired();

        builder.Property(e => e.RevocationReason)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.RevokedAt);
        builder.Property(e => e.LastActivityAt);

        builder.HasIndex(e => e.RefreshToken)
            .HasDatabaseName("IX_UserSessions_RefreshToken")
            .IsUnique();

        builder.HasIndex(e => new { e.UserId, e.DeviceInfo })
            .IsUnique()
            .HasFilter("\"IsRevoked\" = false")
            .HasDatabaseName("IX_UserSessions_UserId_DeviceInfo_Active");

        builder.HasIndex(e => new { e.UserId, e.IsRevoked, e.ExpiresAt });
    }
}