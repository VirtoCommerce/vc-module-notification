using System;
using System.IO;

namespace VirtoCommerce.NotificationsModule.Tests.Common
{
    public class TestUtility
    {
        public static string MapPath(string path)
        {
            var separatorChar = Path.DirectorySeparatorChar;
            var baseDirectory = Directory.GetCurrentDirectory();
            if (baseDirectory.IndexOf($"{separatorChar}bin{separatorChar}", StringComparison.Ordinal) != -1)
            {
                baseDirectory = baseDirectory.Remove(baseDirectory.IndexOf($"{separatorChar}bin{separatorChar}", StringComparison.Ordinal));
            }

            path = path.Replace("~/", "").TrimStart(separatorChar);
            return $"{baseDirectory}{separatorChar}{path}";
        }

        public static string GetStringByPath(string path)
        {
            var fullPath = MapPath(path);
            var content = File.ReadAllText(fullPath);

            return content;
        }
    }
}
