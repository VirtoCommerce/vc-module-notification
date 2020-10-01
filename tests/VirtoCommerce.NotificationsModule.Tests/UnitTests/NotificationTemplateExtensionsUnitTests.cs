using System;
using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationTemplateExtensionsUnitTests
    {
        private readonly List<NotificationTemplate> _templates;
        public NotificationTemplateExtensionsUnitTests()
        {
            _templates = new List<NotificationTemplate>()
            {
                new TestNotificationTemplate { LanguageCode = null },
                new TestNotificationTemplate { LanguageCode = "en-US" },
                new TestNotificationTemplate { LanguageCode = "de-DE" },
            };
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsNull_ReturnDefaultLanguage()
        {
            //Arrange
            string languageCode = null;

            //Act
            var result = _templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Null(result.LanguageCode);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsEmpty_ReturnDefaultLanguage()
        {
            //Arrange
            string languageCode = string.Empty;

            //Act
            var result = _templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Null(result.LanguageCode);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsEnUS_ReturnLanguageEnUS()
        {
            //Arrange
            string languageCode = "en-US";

            //Act
            var result = _templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(languageCode, result.LanguageCode);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsEnUSThereIsNoEn_ReturnDefaultLanguage()
        {
            //Arrange
            string languageCode = "en-US";
            var templates = new List<NotificationTemplate>()
            {
                new TestNotificationTemplate { LanguageCode = null }
            };

            //Act
            var result = templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Null(result.LanguageCode);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenWrongCode_ReturnDefaultLanguage()
        {
            //Arrange
            string languageCode = "fr-FR";

            //Act
            var result = _templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Null(result.LanguageCode);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsEnUS_ReturnLanguageEnUSNotPredefined()
        {
            //Arrange
            string languageCode = "en-US";
            var templates = new List<NotificationTemplate>() {
                new TestNotificationTemplate { LanguageCode = "en-US", IsPredefined = true },
                new TestNotificationTemplate { LanguageCode = "en-US", IsPredefined = false },
            };

            //Act
            var result = templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(languageCode, result.LanguageCode);
            Assert.False(result.IsPredefined);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsEnUS_CouldReturnLanguageEnUSPredefined()
        {
            //Arrange
            string languageCode = "en-US";
            var templates = new List<NotificationTemplate>() {
                new TestNotificationTemplate { LanguageCode = null, IsPredefined = false },
                new TestNotificationTemplate { LanguageCode = "en-US", IsPredefined = true },
            };

            //Act
            var result = templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(languageCode, result.LanguageCode);
            Assert.True(result.IsPredefined);
        }

        [Fact]
        public void FindTemplateForTheLanguage_WhenIsEnUS_ReturnLanguageEnUSNotPredefinedLatest()
        {
            //Arrange
            string languageCode = "en-US";
            var lastDate = new DateTime(2020, 10, 8);
            var templates = new List<NotificationTemplate>() {
                new TestNotificationTemplate { LanguageCode = "en-US", IsPredefined = true, ModifiedDate = lastDate.AddDays(1) },
                new TestNotificationTemplate { LanguageCode = "en-US", IsPredefined = false, ModifiedDate = lastDate.AddDays(-1) },
                new TestNotificationTemplate { LanguageCode = "en-US", IsPredefined = false, ModifiedDate = lastDate },
            };

            //Act
            var result = templates.FindTemplateForLanguage(languageCode);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(languageCode, result.LanguageCode);
            Assert.False(result.IsPredefined);
            Assert.Equal(lastDate, result.ModifiedDate);
        }

    }

    public class TestNotificationTemplate : NotificationTemplate
    {
        public override string Kind => nameof(TestNotificationTemplate);
    }
}
