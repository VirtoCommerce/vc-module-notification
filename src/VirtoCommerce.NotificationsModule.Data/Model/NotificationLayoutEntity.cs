using System.ComponentModel.DataAnnotations;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.NotificationsModule.Data.Model
{
    public class NotificationLayoutEntity : AuditableEntity, IDataEntity<NotificationLayoutEntity, NotificationLayout>
    {
        [Required, StringLength(128)]
        public string Name { get; set; }

        public string Template { get; set; }

        public bool IsDefault { get; set; }

        public NotificationLayoutEntity FromModel(NotificationLayout model, PrimaryKeyResolvingMap pkMap)
        {
            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            Name = model.Name;
            Template = model.Template;
            IsDefault = model.IsDefault;

            return this;
        }

        public NotificationLayout ToModel(NotificationLayout model)
        {
            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.Name = Name;
            model.Template = Template;
            model.IsDefault = IsDefault;

            return model;
        }

        public void Patch(NotificationLayoutEntity target)
        {
            target.Name = Name;
            target.Template = Template;
            target.IsDefault = IsDefault;
        }
    }
}
