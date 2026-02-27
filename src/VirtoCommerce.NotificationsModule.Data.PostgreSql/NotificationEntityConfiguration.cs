using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql;

public class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.Property(x => x.Type).UseCaseInsensitiveCollation();
        builder.Property(x => x.Kind).UseCaseInsensitiveCollation();
    }
}
