using System;
using System.Collections.Generic;
using System.Reflection;
using AquaMai.Config.Attributes;
using DB;
using HarmonyLib;
using MAI2.Util;
using Manager;
using MelonLoader;
using Monitor.Game;
using Process;
using Process.SubSequence;
using UnityEngine;
using UserOption = Manager.UserDatas.UserOption;

namespace AquaMai.Mods.Utils;

[ConfigSection(
    en: "Move answer sound",
    zh: "移动正解音。此设置提供分用户设置，请在游戏内设置中调整偏移量")]
public class MoveAnswerSound
{
    private static float[] userSettings = [0, 0];

    #region 设置界面注入

    public static void OnBeforePatch()
    {
        var field = typeof(OptionCateGameIDEnum).GetField("records", BindingFlags.NonPublic | BindingFlags.Static);
        var originRecords = (OptionCateGameTableRecord[])field.GetValue(null);
        var newRecords = new OptionCateGameTableRecord[8];
        Array.Copy(originRecords, newRecords, originRecords.Length);
        newRecords[7] = new OptionCateGameTableRecord(7, "MoveAnswerSound", "移动正解音", "", "在不修改其他判定时间的情况下，调整正解音的时间\n0.1 => 1毫秒", "");
        field.SetValue(null, newRecords);
    }

    private static Dictionary<UserOption, int> userOptionToPlayer = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionSelectSequence), nameof(OptionSelectSequence.OnStartSequence))]
    public static void OnStartSequence(UserOption ____userOption, int ___PlayerIndex)
    {
        userOptionToPlayer[____userOption] = ___PlayerIndex;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionSelectSequence), nameof(OptionSelectSequence.OnGameStart))]
    public static void OnStartSequence(UserOption ____userOption)
    {
        userOptionToPlayer.Remove(____userOption);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OptionCateGameIDEnum), nameof(OptionCateGameIDEnum.IsValid))]
    public static bool IsValidOption(ref bool __result, OptionCateGameID self)
    {
#if DEBUG
        MelonLogger.Msg($"[MoveAnswerSound] IsValid {self}");
        // MelonLogger.Msg(new StackTrace());
#endif
        __result = (int)self is >= 0 and < 8;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OptionCateGameIDEnum), nameof(OptionCateGameIDEnum.FindID))]
    public static bool FindID(ref OptionCateGameID __result, string enumName)
    {
        if (enumName != "MoveAnswerSound") return true;
        __result = (OptionCateGameID)7;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionSelectSequence), nameof(OptionSelectSequence.GetCategory))]
    public static void GetCategory(ref bool isLeftButtonActive, ref bool isRightButtonActive, string __result)
    {
        if (__result != "移动正解音") return;
        isLeftButtonActive = true;
        isRightButtonActive = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "GetOptionName")]
    private static bool GetOptionName(OptionCategoryID category, int optionIndex, ref string __result)
    {
        if (category != OptionCategoryID.GameSetting || optionIndex != 7) return true;
        __result = "移动正解音";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "GetOptionDetail")]
    private static bool GetOptionDetail(OptionCategoryID category, int optionIndex, ref string __result)
    {
        if (category != OptionCategoryID.GameSetting || optionIndex != 7) return true;
        __result = "在不修改其他判定时间的情况下，调整正解音的时间";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "GetOptionValueIndex")]
    private static bool GetOptionValueIndex(OptionCategoryID category, int currentOptionIndex, ref int __result)
    {
        if (category != OptionCategoryID.GameSetting || currentOptionIndex != 7) return true;
        __result = 114514;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "GetOptionMax")]
    private static bool GetOptionMax(OptionCategoryID category, int currentOptionIndex, ref int __result)
    {
        if (category != OptionCategoryID.GameSetting || currentOptionIndex != 7) return true;
        __result = 1919810;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "GetFilePath")]
    private static bool GetFilePath(OptionCategoryID category, int currentOptionIndex, ref string __result)
    {
        if (category != OptionCategoryID.GameSetting || currentOptionIndex != 7) return true;
        __result = "UI_OPT_E_23_06";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "GetOptionValue")]
    private static bool GetOptionValue(OptionCategoryID category, int optionIndex, ref string __result, string ___DefaultTag, string ___NormalTag, UserOption __instance)
    {
        if (category != OptionCategoryID.GameSetting || optionIndex != 7) return true;
        if (!userOptionToPlayer.TryGetValue(__instance, out var player)) return true;
        __result = $"{(userSettings[player] == 0 ? ___DefaultTag : ___NormalTag)}{userSettings[player]} 毫秒";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "AddOption")]
    private static bool AddOption(OptionCategoryID category, int currentOptionIndex, UserOption __instance)
    {
        if (category != OptionCategoryID.GameSetting || currentOptionIndex != 7) return true;
        if (!userOptionToPlayer.TryGetValue(__instance, out var player)) return true;
#if DEBUG
        MelonLogger.Msg($"[MoveAnswerSound] AddOption {player} {userSettings[player]}");
#endif
        userSettings[player]++;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserOption), "SubOption")]
    private static bool SubOption(OptionCategoryID category, int currentOptionIndex, UserOption __instance)
    {
        if (category != OptionCategoryID.GameSetting || currentOptionIndex != 7) return true;
        if (!userOptionToPlayer.TryGetValue(__instance, out var player)) return true;
#if DEBUG
        MelonLogger.Msg($"[MoveAnswerSound] SubOption {player} {userSettings[player]}");
#endif
        userSettings[player]--;
        return false;
    }

    [HarmonyPatch]
    public class PatchOptionCategoryIDExcenstion
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var clas = Assembly.GetAssembly(typeof(GameCtrl)).GetType("Manager.UserDatas.OptionCategoryIDExcenstion");
            yield return clas.GetMethod("GetCategoryMax");
        }

        public static bool Prefix(ref int __result, OptionCategoryID category)
        {
            if (category == OptionCategoryID.GameSetting)
            {
                __result = 8;
                return false;
            }
            return true;
        }
    }

    #endregion

    #region 设置存储

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MusicSelectProcess), nameof(MusicSelectProcess.OnStart))]
    public static void LoadSettings()
    {
        for (int i = 0; i < 2; i++)
        {
            var userData = UserDataManager.Instance.GetUserData(i);
            if (!userData.IsEntry) continue;
            userSettings[i] = PlayerPrefs.GetFloat($"AquaMaiMoveAnswerSound:{userData.AimeId.Value}", 0);
            MelonLogger.Msg($"玩家 {i} 的移动正解音设置为 {userSettings[i]} 毫秒");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MusicSelectProcess), nameof(MusicSelectProcess.OnRelease))]
    public static void SaveSettings()
    {
        for (int i = 0; i < 2; i++)
        {
            var userData = UserDataManager.Instance.GetUserData(i);
            if (!userData.IsEntry) continue;
            PlayerPrefs.SetFloat($"AquaMaiMoveAnswerSound:{userData.AimeId.Value}", userSettings[i]);
        }
#if DEBUG
        MelonLogger.Msg($"移动正解音设置已保存");
#endif
        PlayerPrefs.Save();
    }

    #endregion

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameCtrl), nameof(GameCtrl.UpdateCtrl))]
    public static void PreUpdateControl(GameObject ____NoteRoot, GameCtrl __instance, NotesManager ___NoteMng)
    {
        if (!____NoteRoot.activeSelf) return;
        var gameScore = Singleton<GamePlayManager>.Instance.GetGameScore(__instance.MonitorIndex);
        if (gameScore.IsTrackSkip) return;
        foreach (NoteData note in ___NoteMng.getReader().GetNoteList())
        {
            if (note == null)
            {
                continue;
            }
            if (note.type.isSlide() || note.type.isConnectSlide()) continue;
            if (NotesManager.GetCurrentMsec() - 33f - userSettings[__instance.MonitorIndex] > note.time.msec && !note.playAnsSoundHead)
            {
                Singleton<GameSingleCueCtrl>.Instance.ReserveAnswerSe(__instance.MonitorIndex);
                note.playAnsSoundHead = true;
            }
            if (note.type.isHold() && NotesManager.GetCurrentMsec() - 33f - userSettings[__instance.MonitorIndex] > note.end.msec && !note.playAnsSoundTail)
            {
                Singleton<GameSingleCueCtrl>.Instance.ReserveAnswerSe(__instance.MonitorIndex);
                note.playAnsSoundTail = true;
            }
        }
    }

    [HarmonyPatch]
    public class PatchIsAllSlide
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(NotesTypeID).GetMethod(nameof(NotesTypeID.isAllSlide), BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// 这个方法只在 GameControl.UpdateCtrl 中用了一次，所以应该可以安全的用它来屏蔽原始的正解音逻辑
        /// </summary>
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}