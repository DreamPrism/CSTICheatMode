using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
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
        private static readonly Stack<InGameCardBase[]> GiveOperations = new();
        private static readonly ManualLogSource PatchLogger = Logger.CreateLogSource("Cheat Mode Patches");
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
            GUILayout.BeginVertical("box");
            GUILayout.Label("Cards");
            if (GUILayout.Button($"Undo last operation{(GiveOperations.Count > 0 ? $": {GiveOperations.Peek()[0].CardModel.CardName}*{GiveOperations.Peek().Length}" : " (None)")}"))
            {
                if (GiveOperations.Count != 0)
                {
                    var lastOperation = GiveOperations.Pop();
                    var cards = lastOperation;
                    for (int i = 0; i < cards.Length; i++)
                    {
                        var card = cards[i];
                        if (!card || !__instance.GM.AllCards.Contains(card)) continue;
                        GameManager.PerformAction(card.CardModel.DefaultDiscardAction, card, true);
                    }
                }
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search");
            __instance.SearchedCardString = GUILayout.TextField(__instance.SearchedCardString);
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
                        GUILayout.BeginHorizontal("box");
                        CardData card = __instance.AllCards[i];
                        GUILayout.Label(string.Format("{0} ({1})", card.CardName, card.name));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Give"))
                        {
                            GiveCardsAndStack(card, false, 1);
                        }
                        if (card.CardType != CardTypes.EnvImprovement)
                        {
                            if (GUILayout.Button("Give 5"))
                                GiveCardsAndStack(card, false, 5);
                            if (card.CardType == CardTypes.Blueprint)
                            {
                                if (GUILayout.Button("Give and unlock"))
                                {
                                    GameManager.GiveCard(card, false);
                                    __instance.GM.BlueprintModelStates[card] = BlueprintModelState.Available;
                                }
                            }
                            if (card.CardType == CardTypes.Item)
                            {
                                if (GUILayout.Button("Give 10"))
                                    GiveCardsAndStack(card, false, 10);
                                if (GUILayout.Button("Give 20"))
                                    GiveCardsAndStack(card, false, 20);
                            }
                        }
                        else if (GUILayout.Button("Give and complete"))
                        {
                            GiveCardsAndStack(card, true);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndScrollView();
            if (string.IsNullOrEmpty(__instance.SearchedCardString))
            {
                GUILayout.BeginHorizontal();
                if (__instance.CurrentPage == 0)
                {
                    GUILayout.Box("<", new GUILayoutOption[] { GUILayout.Width(25f) });
                }
                else if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(25f) }))
                {
                    __instance.CurrentPage--;
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(string.Format("{0}/{1}", (__instance.CurrentPage + 1).ToString(), __instance.MaxPages.ToString()));
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

        private static void GiveCardsAndStack(CardData card, bool complete, int amount = 1)
        {
            var cards = new InGameCardBase[amount];
            for (int i = 0; i < amount; i++)
            {
                GameManager.GiveCard(card, complete);
                var gameCard = MBSingleton<GameManager>.Instance.FindLatestCreatedCard(card);
                cards[i] = gameCard;
            }
            GiveOperations.Push(cards);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "GeneralOptionsGUI")]
        public static bool PatchGeneralOptionsGUI(CheatsManager __instance)
        {
            GUILayout.BeginVertical("box");
            CheatsManager.ShowFPS = GUILayout.Toggle(CheatsManager.ShowFPS, new GUIContent("FPS Counter"));
            CheatsManager.CanDeleteAllCards = GUILayout.Toggle(CheatsManager.CanDeleteAllCards, new GUIContent("All cards can be trashed"));
            Plugin.CombatInvincible = GUILayout.Toggle(Plugin.CombatInvincible, new GUIContent("Be invincible in all combats"));
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Suns ({0})", GameLoad.Instance.SaveData.Suns.ToString()));
            if (GUILayout.Button("+10"))
            {
                GameLoad.Instance.SaveData.Suns += 10;
            }
            if (GUILayout.Button("+100"))
            {
                GameLoad.Instance.SaveData.Suns += 100;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Moons ({0})", GameLoad.Instance.SaveData.Moons.ToString()));
            if (GUILayout.Button("+10"))
            {
                GameLoad.Instance.SaveData.Moons += 10;
            }
            if (GUILayout.Button("+100"))
            {
                GameLoad.Instance.SaveData.Moons += 100;
            }
            GUILayout.EndHorizontal();
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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLoad), "CheckSteamAchievements")]
        public static void PatchSteamAchievementsCheck(GameLoad __instance)
        {
            if (Plugin.HackAchievements && SteamManager.Initialized)
            {
                Plugin.HackAchievements = false;
                PatchLogger.LogInfo("Hacking achievements...");
                foreach (var name in Plugin.AchievementNames)
                {
                    PatchLogger.LogInfo($"Hacking {name}...");
                    SteamUserStats.SetAchievement(name);
                    PatchLogger.LogInfo($"Hacked {name}!");
                }
                SteamUserStats.StoreStats();
                PatchLogger.LogInfo("All achievements done!");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncounterPopup), "RollForClash")]
        public static void PatchInvincibleCombatClash(EncounterPopup __instance)
        {
            var encounter = __instance.CurrentEncounter;
            if (Plugin.CombatInvincible)
                encounter.CurrentRoundClashResult = ClashResults.PlayerHits;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncounterPopup), "GenerateEnemyWound")]
        public static void PatchInvincibleCombatDamage(EncounterPopup __instance)
        {
            if (Plugin.CombatInvincible)
            {
                var encounter = __instance.CurrentEncounter;
                var action = encounter.CurrentPlayerAction;
                if (action.DoesNotAttack || action.Damage.y <= 0f) return;
                encounter.CurrentRoundEnemyWoundSeverity = WoundSeverity.Serious;
                encounter.CurrentRoundEnemyWoundLocation = BodyLocations.Head;
                var wound = encounter.EncounterModel.EnemyBodyTemplate.GetBodyLocation(BodyLocations.Head)
                    .GetWoundsForSeverityDamageType(WoundSeverity.Serious, __instance.CurrentRoundPlayerDamageReport.DmgTypes)
                    .OrderBy(w => w.EnemyValuesModifiers.BloodModifier.y).First();
                encounter.CurrentRoundEnemyWound = wound;
                __instance.CurrentRoundPlayerDamageReport.AttackSeverity = WoundSeverity.Serious;
            }
        }
    }
}
