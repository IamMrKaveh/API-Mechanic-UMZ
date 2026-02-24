namespace Infrastructure.Auth.Configurations;

public sealed class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.OtpHash).IsRequired().HasMaxLength(256);

        builder.HasOne(e => e.User).WithMany(u => u.UserOtps).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}