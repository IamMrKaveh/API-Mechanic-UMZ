namespace Infrastructure.Persistence.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.TokenSelector).IsRequired().HasMaxLength(256);
        builder.Property(e => e.TokenVerifierHash).IsRequired().HasMaxLength(256);
        builder.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.ReplacedByTokenHash).HasMaxLength(256);
        builder.Property(e => e.SessionType).IsRequired().HasMaxLength(50);

        builder.HasIndex(e => e.TokenSelector).IsUnique();
        builder.HasOne(e => e.User).WithMany(u => u.UserSessions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}