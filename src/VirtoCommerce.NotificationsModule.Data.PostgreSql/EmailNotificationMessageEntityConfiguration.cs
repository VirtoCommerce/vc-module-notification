using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.NotificationsModule.Data.Model;

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql;

public class EmailNotificationMessageEntityConfiguration : IEntityTypeConfiguration<EmailNotificationMessageEntity>
{
    public void Configure(EntityTypeBuilder<EmailNotificationMessageEntity> builder)
    {
        builder.Property(x => x.To).UseCollation("case_insensitive");
        builder.Property(x => x.From).UseCollation("case_insensitive");
        builder.Property(x => x.ReplyTo).UseCollation("case_insensitive");
        builder.Property(x => x.Subject).UseCollation("case_insensitive");
        builder.Property(x => x.CC).UseCollation("case_insensitive");
        builder.Property(x => x.BCC).UseCollation("case_insensitive");
        builder.Property(x => x.Body).UseCollation("case_insensitive");
    }
}
