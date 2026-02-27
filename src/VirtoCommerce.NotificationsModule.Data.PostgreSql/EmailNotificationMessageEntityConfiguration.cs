using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql;

public class EmailNotificationMessageEntityConfiguration : IEntityTypeConfiguration<EmailNotificationMessageEntity>
{
    public void Configure(EntityTypeBuilder<EmailNotificationMessageEntity> builder)
    {
        builder.Property(x => x.To).UseCaseInsensitiveCollation();
        builder.Property(x => x.From).UseCaseInsensitiveCollation();
        builder.Property(x => x.ReplyTo).UseCaseInsensitiveCollation();
        builder.Property(x => x.Subject).UseCaseInsensitiveCollation();
        builder.Property(x => x.CC).UseCaseInsensitiveCollation();
        builder.Property(x => x.BCC).UseCaseInsensitiveCollation();
        builder.Property(x => x.Body).UseCaseInsensitiveCollation();
    }
}
