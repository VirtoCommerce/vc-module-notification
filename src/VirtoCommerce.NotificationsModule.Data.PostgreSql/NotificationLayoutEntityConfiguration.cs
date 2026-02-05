using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.NotificationsModule.Data.Model;

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql;

public class NotificationLayoutEntityConfiguration : IEntityTypeConfiguration<NotificationLayoutEntity>
{
    public void Configure(EntityTypeBuilder<NotificationLayoutEntity> builder)
    {
        builder.Property(x => x.Name).UseCollation("case_insensitive");
    }
}
