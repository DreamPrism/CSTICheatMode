using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CstiCheatMode
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static string[] AchievementNames { get; private set; }
        public static bool Enabled { get; private set; }
        public static KeyCode CallConsoleKey { get; private set; }
        public static KeyCode ForceWinGameKey { get; private set; }
        public static KeyCode ForceLoseGameKey { get; private set; }
        public static KeyCode ShowFPSKey { get; private set; }
        public static bool HackAchievements { get; set; }
        public static bool CombatInvincible { get; set; }
        private void Awake()
        {
            Enabled = Config.Bind("General", nameof(Enabled), true, "If true, enable cheat mode.").Value;

            CallConsoleKey = Config.Bind("Hotkeys", nameof(CallConsoleKey), KeyCode.Tab, "The key to call cheat console").Value;
            ForceWinGameKey = Config.Bind("Hotkeys", nameof(ForceWinGameKey), KeyCode.KeypadMultiply, "The key to force win a run").Value;
            ForceLoseGameKey = Config.Bind("Hotkeys", nameof(ForceLoseGameKey), KeyCode.KeypadDivide, "The key to force lose a run").Value;
            ShowFPSKey = Config.Bind("Hotkeys", nameof(ShowFPSKey), KeyCode.F, "The key to show FPS counter").Value;

            HackAchievements = Config.Bind("DangerousZone", nameof(HackAchievements), false, "If true (default false), complete all the achievements on game startup.\nThink twice before changing it to true.").Value;

            if (HackAchievements)
            {
                using var stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("CstiCheatMode.achievements.txt"));
                AchievementNames = stream.ReadToEnd().Split('\n');
                Logger.LogInfo($"Load steam achievement names successfully!");
            }

            Harmony.CreateAndPatchAll(typeof(Localization));
            Harmony.CreateAndPatchAll(typeof(Patches));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
