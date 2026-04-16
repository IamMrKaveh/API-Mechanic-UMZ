using Domain.Media.Aggregates;
using Domain.Media.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Media.Configurations;

public sealed class MediaConfiguration : IEntityTypeConfiguration<Domain.Media.Aggregates.Media>
{
    public void Configure(EntityTypeBuilder<Domain.Media.Aggregates.Media> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasConversion(v => v.Value, v => MediaId.From(v));

        builder.OwnsOne(e => e.Path, pb =>
        {
            pb.Property(p => p.Value)
              .HasColumnName("FilePath")
              .IsRequired()
              .HasMaxLength(1000);

            pb.Property(p => p.FileName)
              .HasColumnName("FileName")
              .IsRequired()
              .HasMaxLength(255);

            pb.Property(p => p.Extension)
              .HasColumnName("FileExtension")
              .HasMaxLength(50);
        });

        builder.OwnsOne(e => e.Size, sb =>
        {
            sb.Property(s => s.Bytes)
              .HasColumnName("FileSize")
              .IsRequired();
        });

        builder.Property(e => e.FileType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EntityId).IsRequired();
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsPrimary).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.AltText).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();

        builder.Ignore(e => e.FilePath);
        builder.Ignore(e => e.FileName);
        builder.Ignore(e => e.Extension);
        builder.Ignore(e => e.FileSize);

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}