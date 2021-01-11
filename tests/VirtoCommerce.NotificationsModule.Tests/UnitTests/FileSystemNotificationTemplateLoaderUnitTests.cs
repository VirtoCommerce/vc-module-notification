using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.NotificationsModule.Tests.Common;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class FileSystemNotificationTemplateLoaderUnitTests
    {
        public FileSystemNotificationTemplateLoaderUnitTests()
        {
            if (!AbstractTypeFactory<NotificationTemplate>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationTemplate)))
                AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            AbstractTypeFactory<Notification>.RegisterType<SampleEmailNotification>();
            AbstractTypeFactory<Notification>.RegisterType<DefaultSampleEmailNotification>();
        }

        [Theory]
        [ClassData(typeof(TemplateTestData))]
        public void LoadTemplates_GetTemplates(string type, string language, string discoveryPath, string defaultPath, string expectedSubject, string expectedBody)
        {
            //Arrange
            var notification = AbstractTypeFactory<Notification>.TryCreateInstance(type);
            notification.LanguageCode = language;
            var templateLoader = GetTemplateLoader();

            //Act
            var templates = templateLoader.LoadTemplates(notification, TestUtility.MapPath(discoveryPath), TestUtility.MapPath(defaultPath));
            var template = templates.FindTemplateForLanguage(language);

            //Assert
            Assert.NotNull(template);
            Assert.Equal(expectedSubject, Assert.IsType<EmailNotificationTemplate>(template).Subject);
            Assert.Equal(expectedBody, Assert.IsType<EmailNotificationTemplate>(template).Body);
        }

        public FileSystemNotificationTemplateLoader GetTemplateLoader()
        {
            var options = new FileSystemTemplateLoaderOptions();
            return new FileSystemNotificationTemplateLoader(Options.Create(options));
        }


        public class TemplateTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { nameof(SampleEmailNotification), null, "Content", Path.Combine("Content", "Default"), "Sample subject\r\n", "<p>Sample Text</p>" };
                yield return new object[] { nameof(SampleEmailNotification), "en-US", "Content", Path.Combine("Content", "Default"), "en-US subject\r\n", "<p>en-US Text</p>" };
                yield return new object[] { nameof(SampleEmailNotification), null, string.Empty, Path.Combine("Content", "Default"), "Sample subject as Default\r\n", "<p>Sample Text as Default</p>" };
                yield return new object[] { nameof(DefaultSampleEmailNotification), null, "Content", Path.Combine("Content", "Default"), "Default subject\r\n", "<p>Default Text</p>" };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
