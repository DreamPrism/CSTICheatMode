﻿using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = BepInEx.Logging.Logger;

namespace CstiCheatMode;

public static class Patches
{
    private static readonly Stack<InGameCardBase[]> GiveOperations = new();
    private static bool ScrollingCards;
    private static bool UnlockingBlueprints;
    private static readonly ManualLogSource PatchLogger = Logger.CreateLogSource("Cheat Mode Patches");

    private static void ClearEmptyCardOperation()
    {
        if (GiveOperations.Count <= 0) return;
        var peek = GiveOperations.Peek();
        while (peek.Length == 0 || peek[0].CardModel == null)
        {
            GiveOperations.Pop();
            if (GiveOperations.Count == 0) break;
            peek = GiveOperations.Peek();
        }
    }

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
        var gm = __instance.GM;
        if (!gm)
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
        GUILayout.Label(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.Cards",
            DefaultText = "Cards"
        });
        ClearEmptyCardOperation();
        if (GUILayout.Button(
                $"{new LocalizedString { LocalizationKey = "CstiCheatMode.Undo", DefaultText = "Undo last operation" }}{(GiveOperations.Count > 0 ? $": {GiveOperations.Peek()[0].CardModel.CardName}*{GiveOperations.Peek().Length}" : " (None)")}"))
        {
            if (GiveOperations.Count != 0)
            {
                var lastOperation = GiveOperations.Pop();
                foreach (var card in lastOperation)
                {
                    if (!card || !gm.AllCards.Contains(card)) continue;
                    GameManager.PerformAction(card.CardModel.DefaultDiscardAction, card, true);
                }
            }
        }

        if (GUILayout.Button($"{new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.UnlockAllBlueprints",
            DefaultText = "Unlock all blueprints"
        }}"))
        {
            foreach (var card in __instance.AllCards)
            {
                if (!card || card.CardType != CardTypes.Blueprint) continue;
                UnlockingBlueprints = true;

                GameManager.GiveCard(card, false);
                if (!gm.CheckedBlueprints.Contains(card))
                    gm.CheckedBlueprints.Add(card);
                if (!gm.BlueprintModelCards.Contains(card))
                    gm.BlueprintModelCards.Add(card);

                gm.BlueprintModelStates[card] = BlueprintModelState.Available;
                if (gm.PurchasableBlueprintCards.Contains(card))
                    gm.PurchasableBlueprintCards.Remove(card);

                UnlockingBlueprints = false;
            }
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.Search",
            DefaultText = "Search"
        });
        __instance.SearchedCardString = GUILayout.TextField(__instance.SearchedCardString);
        GUILayout.EndHorizontal();
        __instance.CardsListScrollView =
            GUILayout.BeginScrollView(__instance.CardsListScrollView, GUILayout.ExpandHeight(true));
        __instance.SearchedCardString ??= "";
        for (var i = 0; i < __instance.AllCards.Length; i++)
        {
            if (!__instance.AllCards[i].name.ToLower().Contains(__instance.SearchedCardString.ToLower()) &&
                !__instance.AllCards[i].CardName.ToString().ToLower()
                    .Contains(__instance.SearchedCardString.ToLower())) continue;
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
                var card = __instance.AllCards[i];
                GUILayout.Label($"{card.CardName} ({card.name})");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new LocalizedString
                    {
                        LocalizationKey = "CstiCheatMode.Give",
                        DefaultText = "Give"
                    }))
                {
                    GiveCardsAndStack(card, false);
                }

                if (card.CardType != CardTypes.EnvImprovement)
                {
                    if (GUILayout.Button(new LocalizedString
                        {
                            LocalizationKey = "CstiCheatMode.Give5",
                            DefaultText = "Give 5"
                        }))
                        GiveCardsAndStack(card, false, 5);
                    if (card.CardType == CardTypes.Blueprint)
                    {
                        if (GUILayout.Button(new LocalizedString
                            {
                                LocalizationKey = "CstiCheatMode.Unlock",
                                DefaultText = "Unlock"
                            }))
                        {
                            GameManager.GiveCard(card, false);
                            gm.BlueprintModelStates[card] = BlueprintModelState.Available;
                            if (gm.PurchasableBlueprintCards.Contains(card))
                                gm.PurchasableBlueprintCards.Remove(card);
                        }
                    }
                    else if (card.CardType == CardTypes.Item)
                    {
                        if (GUILayout.Button(new LocalizedString
                            {
                                LocalizationKey = "CstiCheatMode.Give10",
                                DefaultText = "Give 10"
                            }))
                            GiveCardsAndStack(card, false, 10);
                        if (GUILayout.Button(new LocalizedString
                            {
                                LocalizationKey = "CstiCheatMode.Give20",
                                DefaultText = "Give 20"
                            }))
                            GiveCardsAndStack(card, false, 20);
                    }
                }
                else if (GUILayout.Button(new LocalizedString
                         {
                             LocalizationKey = "CstiCheatMode.Complete",
                             DefaultText = "Give and complete"
                         }))
                {
                    GiveCardsAndStack(card, true);
                }

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndScrollView();
        if (string.IsNullOrEmpty(__instance.SearchedCardString))
        {
            GUILayout.BeginHorizontal();
            if (__instance.CurrentPage == 0)
            {
                GUILayout.Box("<", GUILayout.Width(25f));
            }
            else if (GUILayout.Button("<", GUILayout.Width(25f)))
            {
                __instance.CurrentPage--;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{(__instance.CurrentPage + 1).ToString()}/{__instance.MaxPages.ToString()}");
            GUILayout.FlexibleSpace();
            if (__instance.CurrentPage == __instance.MaxPages - 1)
            {
                GUILayout.Box(">", GUILayout.Width(25f));
            }
            else if (GUILayout.Button(">", GUILayout.Width(25f)))
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
        for (var i = 0; i < amount; i++)
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
        // 可开关功能
        GUILayout.BeginVertical("box");
        // FPS显示
        CheatsManager.ShowFPS = GUILayout.Toggle(CheatsManager.ShowFPS, new GUIContent(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.FPSCounter",
            DefaultText = "FPS Counter"
        }));
        // 可删除所有卡牌
        CheatsManager.CanDeleteAllCards = GUILayout.Toggle(CheatsManager.CanDeleteAllCards, new GUIContent(
            new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.TrashAll",
                DefaultText = "All cards can be trashed"
            }));
        // 快速探索
        Plugin.FastExploration = GUILayout.Toggle(Plugin.FastExploration, new GUIContent(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.FastExploration",
            DefaultText = "Fast exploration"
        }));
        // 战斗无敌
        Plugin.CombatInvincible = GUILayout.Toggle(Plugin.CombatInvincible, new GUIContent(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.EncounterInvincible",
            DefaultText = "Be invincible in all encounters"
        }));

        // 修改太阳和月亮数量
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.Suns",
            DefaultText = "Suns"
        }} ({GameLoad.Instance.SaveData.Suns.ToString()})");
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
        GUILayout.Label($"{new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.Moons",
            DefaultText = "Moons"
        }} ({GameLoad.Instance.SaveData.Moons.ToString()})");
        if (GUILayout.Button("+10"))
        {
            GameLoad.Instance.SaveData.Moons += 10;
        }

        if (GUILayout.Button("+100"))
        {
            GameLoad.Instance.SaveData.Moons += 100;
        }

        GUILayout.EndHorizontal();

        // 打开文件夹相关功能
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.OpenSaveFolder",
                DefaultText = "Open save folder"
            }))
        {
            Application.OpenURL(GameLoad.GameFilesDirectoryPath);
        }

        if (GUILayout.Button(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.OpenLogFolder",
                DefaultText = "Open Player.log folder"
            }))
        {
            Application.OpenURL(Path.Combine(Application.persistentDataPath));
        }

        GUILayout.EndHorizontal();
        // 将存档和日志打包导出到桌面
        if (__instance.GM && GUILayout.Button(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.ExportDiagnosticInfos",
                DefaultText = "Export diagnostic log infos to desktop"
            }))
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var savePath = Path.Combine(desktopPath, $"CSTI_{DateTime.Now:yyyy-MM-dd HH_mm_ss}");
            var currentGameSave = GameLoad.Instance.Games[GameLoad.Instance.CurrentGameDataIndex].FileName;
            var saveDataPath = Path.Combine(GameLoad.GameFilesDirectoryPath, currentGameSave);
            var logPath = Path.Combine(Application.persistentDataPath, "Player.log");
            Directory.CreateDirectory(savePath);
            File.Copy(logPath, Path.Combine(savePath, "Player.log"));
            File.Copy(saveDataPath, Path.Combine(savePath, currentGameSave));
            Application.OpenURL(savePath);
        }

        GUILayout.EndVertical();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CheatsManager), "TimeGUI")]
    public static bool TimeGUI(CheatsManager __instance)
    {
        if (!__instance.GM)
        {
            return false;
        }

        GUILayout.BeginVertical("box");
        GUILayout.Label(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.SetTimeTitle",
            DefaultText = "Set time to:"
        });
        GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
        GUILayout.Label(new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.Days",
            DefaultText = "Days:"
        });
        GUILayout.FlexibleSpace();
        if (GUILayout.RepeatButton("-", GUILayout.Width(25f)) && Time.frameCount % 4 == 0)
        {
            __instance.SetTimeDay--;
        }

        GUILayout.Label(__instance.SetTimeDay.ToString(), GUILayout.Width(37.5f));
        if (GUILayout.RepeatButton("+", GUILayout.Width(25f)) && Time.frameCount % 4 == 0)
        {
            __instance.SetTimeDay++;
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
        GUILayout.Label($"{new LocalizedString
        {
            LocalizationKey = "CstiCheatMode.Ticks",
            DefaultText = "Tick"
        }} ({GameManager.TotalTicksToHourOfTheDayString(GameManager.HoursToTick(__instance.GM.DaySettings.DayStartingHour) + __instance.SetTimeTick, 0)}):");
        GUILayout.FlexibleSpace();
        if (GUILayout.RepeatButton("-", GUILayout.Width(25f)) && Time.frameCount % 4 == 0)
        {
            __instance.SetTimeTick--;
        }

        GUILayout.Label(__instance.SetTimeTick.ToString(), GUILayout.Width(37.5f));
        if (GUILayout.RepeatButton("+", GUILayout.Width(25f)) && Time.frameCount % 4 == 0)
        {
            __instance.SetTimeTick++;
        }

        GUILayout.EndHorizontal();
        if (__instance.GM)
        {
            __instance.SetTimeTick = Mathf.Clamp(__instance.SetTimeTick, 0, __instance.GM.DaySettings.DailyPoints);
            __instance.SetTimeDay = Mathf.Max(0, __instance.SetTimeDay);
        }
        else
        {
            GUILayout.Label("No GameManager found");
        }

        if (GUILayout.Button(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.SetTime",
                DefaultText = "Set time!"
            }) && __instance.GM)
        {
            __instance.GM.SetTimeTo(__instance.SetTimeDay, __instance.SetTimeTick);
        }

        GUILayout.EndVertical();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CheatsManager), "Update")]
    public static bool PatchCheatsUpdate(CheatsManager __instance)
    {
        if (!Plugin.Enabled) return false;
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
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InGameCardBase), "OnPointerClick")]
    public static bool PatchCardOnPointerClick(InGameCardBase __instance, PointerEventData _Pointer)
    {
        if (Input.GetKey(Plugin.MoveCardPileKey) && _Pointer.button == PointerEventData.InputButton.Right)
        {
            var slot = __instance.CurrentSlot;
            var pile = slot.CardPile.ToList();
            if (pile.Count > Plugin.CardPileNoDelayMovingMaxSize)
            {
                var swapStackRoutine = SwapCardPileRoutine(pile);
                slot.GM.StartCoroutine(swapStackRoutine);
                return false;
            }

            if (pile.Count > 1)
            {
                foreach (var card in pile)
                {
                    card.SwapCard();
                }

                return false;
            }

            __instance.SwapCard();
            return false;
        }

        if (CheatsManager.CanDeleteAllCards && Input.GetKey(Plugin.FastDeleteCardKey) &&
            _Pointer.button == PointerEventData.InputButton.Right && __instance &&
            __instance.CardModel && __instance.CardModel.DefaultDiscardAction != null)
        {
            GameManager.PerformAction(__instance.CardModel.DefaultDiscardAction, __instance, false);
            return false;
        }

        return true;
    }

    private static IEnumerator SwapCardPileRoutine(IReadOnlyList<InGameCardBase> cards)
    {
        if (cards.Count == 0) yield break;
        int num;
        for (var i = 0; i < cards.Count; i = num + 1)
        {
            cards[i].SwapCard();
            yield return new WaitForSeconds(Plugin.MoveCardPileDelay);
            num = i;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), "Update")]
    public static void PatchGameUpdate(GameManager __instance)
    {
        ScrollingCards = Input.GetKey(Plugin.ScrollItemKey);
        if (!GameManager.HoveredCard || !ScrollingCards) return;

        var card = GameManager.HoveredCard;

        if ((Input.mouseScrollDelta.y < 0 && card.CurrentSlotInfo.SlotType != SlotsTypes.Item) ||
            (Input.mouseScrollDelta.y > 0 && card.CurrentSlotInfo.SlotType == SlotsTypes.Item))
            card.SwapCard();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScrollRect), "OnScroll")]
    public static void PatchScrollRect(ScrollRect __instance, ref bool __runOriginal)
    {
        __runOriginal = !ScrollingCards;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GraphicsManager), "PlayBlueprintUnlocked")]
    public static void PatchBlockNewBlueprintPopups(GraphicsManager __instance, ref bool __runOriginal)
    {
        __runOriginal = !UnlockingBlueprints;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExplorationBar), "ShouldUnlockExplorationResults")]
    public static bool PatchShouldUnlockExplorationResults(ExplorationBar __instance, ref bool __result)
    {
        if (!Plugin.FastExploration) return true;
        __result = true;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLoad), "CheckSteamAchievements")]
    public static void PatchSteamAchievementsCheck(GameLoad __instance)
    {
        if (!Plugin.HackAchievements || !SteamManager.Initialized) return;
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
        if (!Plugin.CombatInvincible) return;
        var encounter = __instance.CurrentEncounter;
        var action = encounter.CurrentPlayerAction;
        if (action.DoesNotAttack || action.Damage.y <= 0f) return;
        encounter.CurrentRoundEnemyWoundSeverity = WoundSeverity.Serious;
        encounter.CurrentRoundEnemyWoundLocation = BodyLocations.Head;
        var wound = encounter.EncounterModel.EnemyBodyTemplate.GetBodyLocation(BodyLocations.Head)
            .GetWoundsForSeverityDamageType(WoundSeverity.Serious,
                __instance.CurrentRoundPlayerDamageReport.DmgTypes)
            .OrderBy(w => w.EnemyValuesModifiers.BloodModifier.y).First();
        encounter.CurrentRoundEnemyWound = wound;
        __instance.CurrentRoundPlayerDamageReport.AttackSeverity = WoundSeverity.Serious;
    }
}