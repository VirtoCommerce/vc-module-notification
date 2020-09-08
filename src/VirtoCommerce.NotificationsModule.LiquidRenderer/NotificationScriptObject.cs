using Scriban.Runtime;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Localizations;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class NotificationScriptObject : ScriptObject
    {
        public NotificationScriptObject(ITranslationService translationService, IBlobUrlResolver blobUrlResolver)
        {
            TranslationService = translationService;
            BlobUrlResolver = blobUrlResolver;
        }

        public string Language
        {
            get => GetSafeValue<string>(nameof(Language));
            set => SetValue(nameof(Language), value, readOnly: true);
        }

        public ITranslationService TranslationService
        {
            get => GetSafeValue<ITranslationService>(nameof(TranslationService));
            set => SetValue(nameof(TranslationService), value, readOnly: true);
        }

        public IBlobUrlResolver BlobUrlResolver
        {
            get => GetSafeValue<IBlobUrlResolver>(nameof(BlobUrlResolver));
            set => SetValue(nameof(BlobUrlResolver), value, readOnly: true);
        }
    }
}
