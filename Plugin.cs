using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CstiCheatMode
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool Enabled { get; private set; }
        public static KeyCode CallConsoleKey { get; private set; }
        public static KeyCode ForceWinGameKey { get; private set; }
        public static KeyCode ForceLoseGameKey { get; private set; }
        public static KeyCode ShowFPSKey { get; private set; }
        private void Awake()
        {
            Enabled = Config.Bind("General", nameof(Enabled), true, "If true, enable cheat mode.").Value;

            CallConsoleKey = Config.Bind("Hotkeys", nameof(CallConsoleKey), KeyCode.Tab, "The key to call cheat console").Value;
            ForceWinGameKey= Config.Bind("Hotkeys", nameof(ForceWinGameKey), KeyCode.KeypadMultiply, "The key to force win a run").Value;
            ForceLoseGameKey = Config.Bind("Hotkeys", nameof(ForceLoseGameKey), KeyCode.KeypadDivide, "The key to force lose a run").Value;
            ShowFPSKey = Config.Bind("Hotkeys", nameof(ShowFPSKey), KeyCode.F, "The key to show FPS counter").Value;

            Harmony.CreateAndPatchAll(typeof(Patches));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
