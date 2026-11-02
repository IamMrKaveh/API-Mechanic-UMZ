using Domain.Security.Aggregates;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Common.Extensions;

namespace Infrastructure.Auth.Configurations;

public sealed class OtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);

        builder.AddInterceptorRowVersion();

        builder.Property(e => e.UserId)
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

        // Phone-change challenges carry the number they were issued for. Null
        // for every other purpose, so this must stay optional.
        builder.OwnsOne(e => e.TargetPhoneNumber, tp =>
        {
            tp.Property(p => p.Value)
                .HasColumnName("TargetPhoneNumber")
                .HasMaxLength(20);
        });

        builder.HasIndex(e => new { e.UserId, e.Purpose, e.ExpiresAt });
        builder.HasIndex(e => e.CreatedAt);
    }
}
