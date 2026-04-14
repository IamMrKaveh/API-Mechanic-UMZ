using Domain.Security.Aggregates;

namespace Infrastructure.Auth.Configurations;

public sealed class OtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property(e => e.PhoneNumber)
            .HasConversion(v => v.Value, v => Domain.Security.ValueObjects.PhoneNumber.From(v))
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(e => e.CodeHash).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Purpose).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.Purpose, e.ExpiresAt });
    }
}