namespace Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Domain.Cart.Cart>
{
    public void Configure(EntityTypeBuilder<Domain.Cart.Cart> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.GuestToken).HasMaxLength(256);

        builder.HasOne(e => e.User).WithMany(u => u.UserCarts).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}