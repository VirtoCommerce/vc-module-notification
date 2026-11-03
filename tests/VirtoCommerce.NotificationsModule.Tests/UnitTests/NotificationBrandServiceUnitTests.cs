using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;
using BrandSettings = VirtoCommerce.NotificationsModule.Core.ModuleConstants.Settings.Brand;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationBrandServiceUnitTests
    {
        private readonly Dictionary<string, object> _globalValues = new();
        private readonly Dictionary<string, object> _storeValues = new();
        private readonly NotificationBrandService _service;

        public NotificationBrandServiceUnitTests()
        {
            var settingsManager = new Mock<ISettingsManager>();
            settingsManager
                .Setup(x => x.GetObjectSettingsAsync(It.IsAny<IEnumerable<string>>(), null, null))
                .ReturnsAsync(() => Entries(_globalValues));
            settingsManager
                .Setup(x => x.GetObjectSettingsAsync(It.IsAny<IEnumerable<string>>(), "Store", "store-1"))
                .ReturnsAsync(() => Entries(_storeValues));
            _service = new NotificationBrandService(settingsManager.Object);
        }

        [Fact]
        public async Task GetBrandAsync_NothingSet_Defaults()
        {
            var brand = await _service.GetBrandAsync("store-1");

            Assert.Null(brand.LogoText);
            Assert.Null(brand.LogoUrl);
            Assert.Equal(160, brand.LogoWidth);
            Assert.Equal("#e8912a", brand.AccentColor);
            Assert.Equal("line", brand.HeaderStyle);
            Assert.Equal(600, brand.EmailWidth);
            Assert.Empty(brand.SocialLinks);
            Assert.False(brand.ShowUnsubscribe);
        }

        [Fact]
        public async Task GetBrandAsync_StoreValueSet_StoreWinsOverGlobal()
        {
            _globalValues[BrandSettings.AccentColor.Name] = "#111111";
            _globalValues[BrandSettings.LogoText.Name] = "Global";
            _storeValues[BrandSettings.AccentColor.Name] = "#3b7ddd";
            _storeValues[BrandSettings.LogoText.Name] = "  ";

            var brand = await _service.GetBrandAsync("store-1");

            Assert.Equal("#3b7ddd", brand.AccentColor);
            Assert.Equal("Global", brand.LogoText);
        }

        [Fact]
        public async Task GetBrandAsync_NoStore_GlobalOnly()
        {
            _globalValues[BrandSettings.HeaderStyle.Name] = "dark";
            _storeValues[BrandSettings.HeaderStyle.Name] = "accent";

            var brand = await _service.GetBrandAsync(null);

            Assert.Equal("dark", brand.HeaderStyle);
        }

        [Fact]
        public async Task GetBrandAsync_LinksJson_Parsed()
        {
            _storeValues[BrandSettings.SocialLinks.Name] = "[{\"name\":\"LinkedIn\",\"url\":\"https://linkedin.com/acme\"},{\"name\":\"Empty\"}]";
            _storeValues[BrandSettings.UtilityLinks.Name] = "not json";
            _storeValues[BrandSettings.EmailWidth.Name] = 700L;
            _storeValues[BrandSettings.ShowUnsubscribe.Name] = true;

            var brand = await _service.GetBrandAsync("store-1");

            var link = Assert.Single(brand.SocialLinks);
            Assert.Equal("LinkedIn", link.Name);
            Assert.Equal("https://linkedin.com/acme", link.Url);
            Assert.Empty(brand.UtilityLinks);
            Assert.Equal(700, brand.EmailWidth);
            Assert.True(brand.ShowUnsubscribe);
        }

        private static IEnumerable<ObjectSettingEntry> Entries(Dictionary<string, object> values)
        {
            return BrandSettings.AllSettings
                .Select(x => new ObjectSettingEntry(x) { Value = values.GetValueOrDefault(x.Name) })
                .ToList();
        }
    }
}
