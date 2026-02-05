using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.NotificationsModule.Data.Model;

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql;

public class NotificationMessageEntityConfiguration : IEntityTypeConfiguration<NotificationMessageEntity>
{
    public void Configure(EntityTypeBuilder<NotificationMessageEntity> builder)
    {
        builder.Property(x => x.Status).UseCollation("case_insensitive");
        builder.Property(x => x.NotificationType).UseCollation("case_insensitive");
        builder.Property(x => x.LastSendError).UseCollation("case_insensitive");
    }
}
