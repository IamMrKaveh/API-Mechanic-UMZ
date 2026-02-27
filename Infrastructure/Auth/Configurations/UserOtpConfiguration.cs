namespace Infrastructure.Auth.Configurations;

public sealed class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.OtpHash).IsRequired().HasMaxLength(256);

        builder.Property(e => e.ExpiresAt)
            .HasField("_expiresAt")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.IsUsed)
            .HasField("_isUsed")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.UsedAt)
            .HasField("_usedAt")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.AttemptCount)
            .HasField("_attemptCount")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(e => e.User)
            .WithMany(u => u.UserOtps)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}