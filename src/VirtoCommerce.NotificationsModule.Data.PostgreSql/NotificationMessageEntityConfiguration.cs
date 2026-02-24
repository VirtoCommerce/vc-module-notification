using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql;

public class NotificationMessageEntityConfiguration : IEntityTypeConfiguration<NotificationMessageEntity>
{
    public void Configure(EntityTypeBuilder<NotificationMessageEntity> builder)
    {
        builder.Property(x => x.Status).UseCaseInsensitiveCollation();
        builder.Property(x => x.NotificationType).UseCaseInsensitiveCollation();
        builder.Property(x => x.LastSendError).UseCaseInsensitiveCollation();
    }
}
