namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class NotificationRenderContext
    {
        public string Template { get; set; }

        public object Model { get; set; }

        public string Language { get; set; }

        public string LayoutId { get; set; }

        public bool UseLayouts { get; set; }
    }
}
