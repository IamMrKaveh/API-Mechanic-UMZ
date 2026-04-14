using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        builder.Property(e => e.Code)
            .HasConversion(v => v.Value, v => OtpCode.Create(v))
            .IsRequired()
            .HasMaxLength(8);

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