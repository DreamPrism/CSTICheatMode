using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace CstiCheatMode;

public static class Localization
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocalizationManager), "LoadLanguage")]
    public static void LoadLanguagePostfix()
    {
            var instance = LocalizationManager.Instance;
            if (instance == null || instance.Languages == null ||
                LocalizationManager.CurrentLanguage >= instance.Languages.Length) return;
            var langSetting = instance.Languages[LocalizationManager.CurrentLanguage];
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"CstiCheatMode.locale.{langSetting.LanguageName}.csv");
            if (stream == null || !stream.CanRead) return;
            using StreamReader reader = new(stream);
            var localizationString = reader.ReadToEnd();
            var dictionary = CSVParser.LoadFromString(localizationString);

            Regex regex = new(@"\\n");
            var currentTexts = LocalizationManager.CurrentTexts;
            foreach (var item in dictionary.Where(item => !currentTexts.ContainsKey(item.Key) && item.Value.Count >= 2))
                currentTexts.Add(item.Key, regex.Replace(item.Value[1], "\n"));
    }
}