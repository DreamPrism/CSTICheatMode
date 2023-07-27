using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace CstiCheatMode
{
    public static class Patches
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource("Cheat Mode Patches");
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "CheatsActive", MethodType.Getter)]
        public static bool PatchCheatsActive(ref bool __result)
        {
            __result = Plugin.Enabled;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "CardsGUI")]
        public static bool PatchCardsGUI(CheatsManager __instance)
        {
            if (!__instance.GM)
            {
                return false;
            }
            if (__instance.AllCards == null)
            {
                __instance.FillCards();
            }
            if (__instance.AllCards == null)
            {
                return false;
            }
            if (__instance.AllCards.Length == 0)
            {
                return false;
            }
            GUILayout.BeginVertical("box", Array.Empty<GUILayoutOption>());
            GUILayout.Label("Cards", Array.Empty<GUILayoutOption>());
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label("Search", Array.Empty<GUILayoutOption>());
            __instance.SearchedCardString = GUILayout.TextField(__instance.SearchedCardString, Array.Empty<GUILayoutOption>());
            GUILayout.EndHorizontal();
            __instance.CardsListScrollView = GUILayout.BeginScrollView(__instance.CardsListScrollView, new GUILayoutOption[] { GUILayout.ExpandHeight(true) });
            if (__instance.SearchedCardString == null)
            {
                __instance.SearchedCardString = "";
            }
            for (int i = 0; i < __instance.AllCards.Length; i++)
            {
                if (__instance.AllCards[i].name.ToLower().Contains(__instance.SearchedCardString.ToLower()) || __instance.AllCards[i].CardName.ToString().ToLower().Contains(__instance.SearchedCardString.ToLower()))
                {
                    if (i / 150 != __instance.CurrentPage && string.IsNullOrEmpty(__instance.SearchedCardString))
                    {
                        if (i >= 150 * __instance.CurrentPage)
                        {
                            break;
                        }
                    }
                    else
                    {
                        GUILayout.BeginHorizontal("box", Array.Empty<GUILayoutOption>());
                        GUILayout.Label(string.Format("{0} ({1})", __instance.AllCards[i].CardName, __instance.AllCards[i].name), Array.Empty<GUILayoutOption>());
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Give", Array.Empty<GUILayoutOption>()))
                        {
                            GameManager.GiveCard(__instance.AllCards[i], false);
                        }
                        if (__instance.AllCards[i].CardType != CardTypes.EnvImprovement)
                        {
                            if (GUILayout.Button("Give 5", Array.Empty<GUILayoutOption>()))
                            {
                                for (int j = 0; j < 5; j++)
                                {
                                    GameManager.GiveCard(__instance.AllCards[i], false);
                                }
                            }
                        }
                        else if (GUILayout.Button("Give and complete", Array.Empty<GUILayoutOption>()))
                        {
                            GameManager.GiveCard(__instance.AllCards[i], true);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndScrollView();
            if (string.IsNullOrEmpty(__instance.SearchedCardString))
            {
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                if (__instance.CurrentPage == 0)
                {
                    GUILayout.Box("<", new GUILayoutOption[] { GUILayout.Width(25f) });
                }
                else if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(25f) }))
                {
                    __instance.CurrentPage--;
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(string.Format("{0}/{1}", (__instance.CurrentPage + 1).ToString(), __instance.MaxPages.ToString()), Array.Empty<GUILayoutOption>());
                GUILayout.FlexibleSpace();
                if (__instance.CurrentPage == __instance.MaxPages - 1)
                {
                    GUILayout.Box(">", new GUILayoutOption[] { GUILayout.Width(25f) });
                }
                else if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(25f) }))
                {
                    __instance.CurrentPage++;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "Update")]
        public static bool PatchCheatsUpdate(CheatsManager __instance)
        {
            if (Plugin.Enabled)
            {
                if (!__instance.GM)
                {
                    __instance.GM = MBSingleton<GameManager>.Instance;
                }
                if (Input.GetKeyDown(Plugin.ShowFPSKey))
                {
                    CheatsManager.ShowFPS = !CheatsManager.ShowFPS;
                }
                if (__instance.GM)
                {
                    if (Input.GetKeyDown(Plugin.ForceLoseGameKey))
                    {
                        MBSingleton<GameManager>.Instance.OpenEndgameJournal(true);
                    }
                    else if (Input.GetKeyDown(Plugin.ForceWinGameKey))
                    {
                        MBSingleton<GameManager>.Instance.OpenEndgameJournal(false);
                    }
                }
                if (Input.GetKeyDown(Plugin.CallConsoleKey))
                {
                    __instance.ShowGUI = !__instance.ShowGUI;
                }
                __instance.CheatsMenuBGObject.SetActive(__instance.ShowGUI);
            }
            return false;
        }
    }
}
