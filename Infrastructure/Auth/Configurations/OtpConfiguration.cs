using Domain.Security.Aggregates;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Configurations;

public sealed class OtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => OtpId.From(v));

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(e => e.CodeHash)
               .HasColumnName("CodeHash")
               .IsRequired()
               .HasMaxLength(128);

        builder.Property(e => e.Purpose)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IsVerified).IsRequired();
        builder.Property(e => e.VerificationAttempts).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.VerifiedAt);

        builder.HasIndex(e => new { e.UserId, e.Purpose, e.ExpiresAt });
        builder.HasIndex(e => e.CreatedAt);
    }
}