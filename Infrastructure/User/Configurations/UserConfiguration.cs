namespace Infrastructure.User.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<Domain.User.User>
{
    public void Configure(EntityTypeBuilder<Domain.User.User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
        builder.Property(e => e.FirstName).HasMaxLength(50);
        builder.Property(e => e.LastName).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(256);

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => e.PhoneNumber).IsUnique();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}