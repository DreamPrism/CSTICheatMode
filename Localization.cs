using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CstiCheatMode
{
    public static class Localization
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LocalizationManager), "LoadLanguage")]
        public static void LoadLanguagePostfix()
        {
            LocalizationManager __instance = LocalizationManager.Instance;
            if (__instance == null || __instance.Languages == null ||
                LocalizationManager.CurrentLanguage >= __instance.Languages.Length) return;
            LanguageSetting langSetting = __instance.Languages[LocalizationManager.CurrentLanguage];
            using Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"CstiCheatMode.locale.{langSetting.LanguageName}.csv");
            if (stream == null || !stream.CanRead) return;
            using StreamReader reader = new(stream);
            string localizationString = reader.ReadToEnd();
            var dictionary = CSVParser.LoadFromString(localizationString, Delimiter.Comma);

            Regex regex = new("\\\\n");
            Dictionary<string, string> currentTexts = LocalizationManager.CurrentTexts;
            foreach (var item in dictionary)
                if (!currentTexts.ContainsKey(item.Key) && item.Value.Count >= 2)
                    currentTexts.Add(item.Key, regex.Replace(item.Value[1], "\n"));
        }
    }
}
