namespace Infrastructure.Common.Extensions;

public static class RowVersionConfigurationExtensions
{
    public const string RowVersionColumnName = "RowVersion";

    public static PropertyBuilder<byte[]> AddInterceptorRowVersion<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        var prop = builder.Property<byte[]>(RowVersionColumnName)
            .IsConcurrencyToken()
            .IsRequired()
            .ValueGeneratedOnAddOrUpdate();

        prop.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
        prop.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

        return prop;
    }

    public static PropertyBuilder<byte[]> AddInterceptorRowVersion(
        this OwnedNavigationBuilder builder)
    {
        var prop = builder.Property<byte[]>(RowVersionColumnName)
            .IsConcurrencyToken()
            .IsRequired()
            .ValueGeneratedOnAddOrUpdate();

        prop.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
        prop.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

        return prop;
    }
}
