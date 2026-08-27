using System;
using System.Globalization;

namespace RoundedTB
{
    /// <summary>
    /// Small runtime language switcher for the tray application. Strings are kept
    /// next to the controls because the project has no resource-based UI layer.
    /// </summary>
    public static class Localization
    {
        public const string English = "en-US";
        public const string SimplifiedChinese = "zh-CN";

        public static string Current { get; private set; } = Detect();

        public static bool IsChinese => string.Equals(Current, SimplifiedChinese, StringComparison.OrdinalIgnoreCase);

        public static string Detect()
        {
            return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? SimplifiedChinese
                : English;
        }

        public static void SetLanguage(string language)
        {
            Current = string.Equals(language, SimplifiedChinese, StringComparison.OrdinalIgnoreCase)
                ? SimplifiedChinese
                : English;
        }

        public static string Text(string english, string chinese)
        {
            return IsChinese ? chinese : english;
        }
    }
}
