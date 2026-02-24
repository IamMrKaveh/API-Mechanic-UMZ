namespace Infrastructure.Review.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.Comment).HasMaxLength(1000);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
        builder.Property(e => e.AdminReply).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasOne(e => e.User).WithMany(u => u.Reviews).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}