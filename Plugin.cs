using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CstiCheatMode;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static string[] AchievementNames { get; private set; }
    public static bool Enabled { get; private set; }
    public static KeyCode CallConsoleKey { get; private set; }
    public static KeyCode ForceWinGameKey { get; private set; }
    public static KeyCode ForceLoseGameKey { get; private set; }
    public static KeyCode ShowFPSKey { get; private set; }
    public static KeyCode FastDeleteCardKey { get; private set; }
    public static KeyCode ScrollItemKey { get; private set; }
    public static KeyCode MoveCardPileKey { get; private set; }
    public static bool HackAchievements { get; set; }
    public static bool CombatInvincible { get; set; }
    public static bool FastExploration { get; set; }
    public static float MoveCardPileDelay { get; set; }
    public static int CardPileNoDelayMovingMaxSize { get; set; }

    private void Awake()
    {
        Enabled = Config.Bind("Cheat-General", nameof(Enabled), true, "If true, enable cheat mode.").Value;

        CallConsoleKey = Config.Bind("Cheat-Hotkeys", nameof(CallConsoleKey), KeyCode.Tab,
                "The key to call cheat console")
            .Value;
        ForceWinGameKey = Config.Bind("Cheat-Hotkeys", nameof(ForceWinGameKey), KeyCode.KeypadMultiply,
            "The key to force win a run").Value;
        ForceLoseGameKey = Config.Bind("Cheat-Hotkeys", nameof(ForceLoseGameKey), KeyCode.KeypadDivide,
            "The key to force lose a run").Value;
        ShowFPSKey = Config.Bind("Cheat-Hotkeys", nameof(ShowFPSKey), KeyCode.F, "The key to show FPS counter").Value;
        FastDeleteCardKey = Config.Bind("Cheat-Hotkeys", nameof(FastDeleteCardKey), KeyCode.X,
            "The key to press when fast deleting cards.").Value;

        MoveCardPileDelay = Config.Bind("QoL-Settings", nameof(MoveCardPileDelay), 0.005f,
                "The delay between moving each card when moving card pile. Default 0.005s, set to 0 to disable delay.")
            .Value;
        CardPileNoDelayMovingMaxSize = Config.Bind("QoL-Settings", nameof(CardPileNoDelayMovingMaxSize), 10,
                "The max size of card pile that can be moved without delay. Value too big can cause lag or crash.")
            .Value;
        ScrollItemKey = Config.Bind("QoL-HotKeys", nameof(ScrollItemKey), KeyCode.LeftAlt,
            "The key to press when using mouse scroll to move cards.").Value;
        MoveCardPileKey = Config.Bind("QoL-HotKeys", nameof(MoveCardPileKey), KeyCode.LeftShift,
            "The key to press when moving card pile.").Value;

        HackAchievements = Config.Bind("DangerousZone", nameof(HackAchievements), false,
                "If true (default false), complete all the achievements on game startup.\nThink twice before changing it to true.")
            .Value;

        if (HackAchievements)
        {
            using var stream = new StreamReader(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("CstiCheatMode.achievements.txt") ?? throw new InvalidOperationException());
            AchievementNames = stream.ReadToEnd().Split('\n');
            Logger.LogInfo($"Load steam achievement names successfully!");
        }

        Harmony.CreateAndPatchAll(typeof(Localization));
        Harmony.CreateAndPatchAll(typeof(Patches));

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}