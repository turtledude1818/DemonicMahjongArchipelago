using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MaJiang;
using MaJiang.Achievement;
using MaJiang.Achievement.Runtime;
using MaJiang.DataConstruct;
using MaJiang.DataConstruct.BaoLingPai;
using MaJiang.DataConstruct.Character;
using MaJiang.DataConstruct.MaJiang;
using MaJiang.DataConstruct.Offering;
using MaJiang.DataConstruct.Relic;
using MaJiang.DataConstruct.XiaoChouPai;
using MaJiang.GameMap;
using MaJiang.GM;
using MaJiang.Log;
using MaJiang.PlayMaJiang.Player.Relic;
using MaJiang.PlayMaJiang.UI.Bag;
using MaJiang.PlayMaJiang.UI.Illustration;
using MaJiang.UI;
using MaJiang.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

namespace DemonicMahjongArchipelago
{
    class BlockingPatches
    {

        [HarmonyPatch(typeof(MaJiang.GM.Saver), "UnlockRelic", new Type[] { typeof(RelicId[]) })]
        [HarmonyPrefix]
        static bool blockRelicUnlock(Saver __instance, RelicId[] relicIds)
        {
            foreach (RelicId id in relicIds)
            {
                ArchipelagoPlugin.BepinLogger.LogInfo($"Blocking unlock of relic {id}");
            }
            return false;
        }
        [HarmonyPatch(typeof(MaJiang.GM.Saver), "UnlockLingYong", new Type[] { typeof(XiaoChou[]) })]
        [HarmonyPrefix]
        static bool blockLingYongUnlock(Saver __instance, XiaoChou[] lingYongIds)
        {
            foreach (XiaoChou id in lingYongIds)
            {
                ArchipelagoPlugin.BepinLogger.LogInfo($"Blocking unlock of figurine {id}");
            }
            return false;
        }
        //[HarmonyPatch(typeof(MaJiang.AccountGameDataHandle), "TryAddCharacter")]
        //[HarmonyPrefix]
        //static bool blockCharacterUnlock(AccountGameDataHandle __instance, bool __result, CharacterID characterId
        //    ,bool writeSave = true)
        //{
        //    ArchipelagoPlugin.BepinLogger.LogInfo($"Blocking unlock of character {characterId}");
        //    __result = !GameData.ReceivedCharacters.Contains( characterId );
        //    return false;
        //}
        [HarmonyPatch(typeof(GameMapMgr), "TryCharacterUnlock")]
        [HarmonyPrefix]
        static bool blockDiffCharUnlock(CharacterID characterID, bool useUnlockEffect)
        {
            ArchipelagoPlugin.BepinLogger.LogInfo($"Blocking unlock of character {characterID}");
            return false;
        }
        [HarmonyPatch(typeof(AchievementRuntime), "TryUnlockProp")]
        [HarmonyPrefix]
        static bool blockAchievementPropUnlock(AchievementRuntime __instance)
        {
            ArchipelagoPlugin.BepinLogger.LogInfo($"Blocking unlock from achievement {__instance.Name}");
            return false;
        }
    }

    class OverridePatches
    {
        /// <summary>
        /// Display only unlocked characters on pre-game character select menu
        /// </summary>
        /// <param name="__result"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(GameMapUtil), "GetUnLockCharacterList")]
        [HarmonyPrefix]
        static bool CharacterSelect(ref Il2CppSystem.Collections.Generic.List<CharacterID> __result)
        {
            __result = new Il2CppSystem.Collections.Generic.List<CharacterID>();
            //foreach (CharacterID id in GameData.startingCharacters.Concat(GameData.receivedCharacters))
            foreach (CharacterID id in GameData.ReceivedCharacters)
            {
                __result.Add(id);
            }
            return false;
        }
        
        /// <summary>
        /// Reimplemenation of base method to choose unlocked characters
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(CharacterLevelChooseCtrl), "RefreshCharacterItem")]
        [HarmonyPrefix]
        static bool RefreshCharacterItem(CharacterLevelChooseCtrl __instance)
        {
            var unlocked = GameData.UnlockedChars();
            var showIndex = __instance.characterShowIndex;
            var itemDict = __instance.characterItemDict;
            var saveData = AccountGameDataHandle.Instance.GetPlayerCharcterSaveDatas();
        
            // Selecting unlocked chars
            __instance.unLockList.Clear();
            foreach (CharacterID id in GameData.UnlockedChars())
            {
                __instance.unLockList.Add(id);
                itemDict[id].SetUnlock(true);
            }
            ArchipelagoPlugin.BepinLogger.LogInfo("Character Level Select altered");
        
        
            // Reimplementation
            foreach (var data in saveData)
            {
                var id = data.CharacterID;
                if (unlocked.Contains(id)) itemDict[id].SetSaveData(data);
            }
        
            __instance.countUnlockCharacter.text = unlocked.Length.ToString();
            for (int i = 0; i < showIndex._size; i++)
            {
                for (int index = 0; index < showIndex._size - i - 1; index++)
                {
                    if (showIndex._size - i - 1 <= index) break;
                    var char1 = showIndex[index];
                    var char2 = showIndex[index + 1];
                    if (__instance.ShouldSwap(char1, char2))
                    {
                        showIndex[index] = char2;
                        showIndex[index + 1] = char1;
                    }
                }
            }
            for (int i = 0; i < showIndex._size; i++)
            {
                if (i == 0) __instance.firstShowCharacter = showIndex[0];
                itemDict[showIndex[i]].Transform.SetSiblingIndex(i);
            }
        
            return false;
        }

        [HarmonyPatch(typeof(MaJiang.GlobalDataCenter), "get_AvailableRelicTotalList")]
        [HarmonyPrefix]
        public static bool AvailableRelicTotalList(
            ref object __result)
            //ref Il2CppSystem.Collections.Generic.IEnumerable<RelicDisplay> __result)
        {
            var list = new Il2CppSystem.Collections.Generic.List<RelicDisplay>();
            var totallist = MaJiang.GlobalDataCenter.Instance.staticDataMgr._relicDisplayList;
            for (int i = 0; i < totallist.Count; i++)
            {
                RelicId id = totallist[i].displayId;
                //if (GameData.startingRelics.Concat(GameData.receivedRelics).Contains(id))
                if (GameData.ReceivedRelics.Contains(id))
                    {
                    list.Add(totallist[i]);
                }
            }
            __result = list;
            return false;
        }
    }
    class ReplaceFieldPatches
    {
        private static XiaoChou[] xiaoChouNeedUnlocked;
        private static Il2CppSystem.Collections.Generic.List<XiaoChou> xiaoChouNeedUnlockedList;
        private static int[] relicNeedUnlocked;
        private static Il2CppSystem.Collections.Generic.List<int> relicNeedUnlockedList;

        // Figurines
        public static void makeStartingFigurines()
        {
            xiaoChouNeedUnlocked = Enum.GetValues<XiaoChou>().ToArray<XiaoChou>();
            xiaoChouNeedUnlockedList = new Il2CppSystem.Collections.Generic.List<XiaoChou>();
            foreach (var f in xiaoChouNeedUnlocked)
            {
                xiaoChouNeedUnlockedList.Add(f);
            }
        }
        [HarmonyPatch(typeof(XiaoChouPaiNeedUnLockedList), "Item", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool XiaoChouNeedUnLockedListGet(int index, ref XiaoChou __result)
        {
            if (xiaoChouNeedUnlocked == null) makeStartingFigurines();
            __result = xiaoChouNeedUnlocked[index];
            return false;
        }
        //[HarmonyPatch(typeof(XiaoChouPaiNeedUnLockedList), "Count", MethodType.Getter)]
        //[HarmonyPrefix]
        //public static bool XiaoChouNeedUnLockedListCount(ref int __result)
        //{
        //    if (xiaoChouNeedUnlocked == null) makeStartingFigurines();
        //    __result = xiaoChouNeedUnlocked.Length;
        //    return false;
        //}
        //[HarmonyPatch(typeof(XiaoChouPaiNeedUnLockedList), "XiaoChouIds", MethodType.Getter)]
        //[HarmonyPrefix]
        //public static bool XiaoChouNeedUnLockedListXiaoChouIds(ref Il2CppArrayBase<XiaoChou> __result)
        //{
        //    if (xiaoChouNeedUnlocked == null) makeStartingFigurines();
        //    __result = xiaoChouNeedUnlockedList.ToArray();
        //    return false;
        //}
        [HarmonyPatch(typeof(XiaoChouPaiNeedUnLockedList), "GetEnumerator")]
        [HarmonyPrefix]
        public static bool XiaoChouNeedUnLockedListEnumerator(
            ref object __result)
        {
            if (xiaoChouNeedUnlocked == null) makeStartingFigurines();
            __result = xiaoChouNeedUnlockedList.GetEnumerator();
            return false;
        }

        //Relics
        public static void makeStartingRelics()
        {
            //relicNeedUnlocked = Enum.GetValues<RelicId>().Except<RelicId>(GameData.startingRelics).
            //    Select<RelicId, int>(relic => (int)relic).ToArray<int>();
            relicNeedUnlocked = Enum.GetValues<RelicId>().Select<RelicId, int>(relic => (int)relic).ToArray<int>();
            var list = new Il2CppSystem.Collections.Generic.List<int>();
            foreach (var f in relicNeedUnlocked)
            {
                list.Add((int)f);
            }
            relicNeedUnlockedList = list;
        }
        [HarmonyPatch(typeof(RelicNeedUnLockedList), "Item", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool RelicNeedUnLockedListGet(int index, ref int __result)
        {
            if (relicNeedUnlocked == null) makeStartingRelics();
            __result = relicNeedUnlocked[index];
            return false;
        }
        //[HarmonyPatch(typeof(RelicNeedUnLockedList), "Count", MethodType.Getter)]
        //[HarmonyPrefix]
        //public static bool RelicNeedUnLockedListCount(ref int __result, ref bool __runOriginal)
        //{
        //    if (relicNeedUnlocked == null) makeStartingRelics();
        //    __result = relicNeedUnlocked.Length;
        //    __runOriginal = false;
        //    return false;
        //}
        //[HarmonyPatch(typeof(RelicNeedUnLockedList), "value", MethodType.Getter)]
        //[HarmonyPrefix]
        //public static bool RelicIdNeedUnLockedListValue(ref Il2CppArrayBase<int> __result)
        //{
        //    if (relicNeedUnlocked == null) makeStartingRelics();
        //    __result = relicNeedUnlockedList.ToArray();
        //    return false;
        //}
        [HarmonyPatch(typeof(RelicNeedUnLockedList), "GetEnumerator")]
        [HarmonyPrefix]
        public static bool RelicIdNeedUnLockedListEnumerator(
            ref object __result)
        {
            if (relicNeedUnlocked == null) makeStartingRelics();
            __result = relicNeedUnlockedList.GetEnumerator();
            return false;
        }
    }

    class ReversePatches
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Saver), "UnlockRelic", new Type[] {typeof(Il2CppStructArray<RelicId>) })]
        public static void UnlockRelic(object __instance, Il2CppStructArray<RelicId> relicIds)
        {
            throw new NotImplementedException();
        }
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Saver), "UnlockLingYong", new Type[] { typeof(Il2CppStructArray<XiaoChou>) })]
        public static void UnlockLingYong(object __instance, Il2CppStructArray<XiaoChou> lingYongIds)
        {
            throw new NotImplementedException();
        }
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(AccountGameDataHandle), "TryAddCharacter")]
        public static bool TryAddCharacter(object __instance, CharacterID characterId, bool writeSave = true)
        {
            throw new NotImplementedException();
        }
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(PopUIManager), "OpenUI", typeof(GameObject), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
        public static IManagedUI OpenUI(PopUIManager __instance, GameObject prefab, Il2CppReferenceArray<Il2CppSystem.Object> openParams = null)
        {
            throw new NotImplementedException();
        }
    }
    class CheckingPatches
    {
        [HarmonyPatch(typeof(Saver), "PlayFanNewFoundAdd")]
        [HarmonyPostfix]
        public static void NewYaku(FanZhong fanZhong)
        {
            GameData.checkLocation(fanZhong, "Yaku");
            GameData.CheckedYaku.Add(fanZhong);
        }

        [HarmonyPatch(typeof(AchievementRuntime), "Get")]
        [HarmonyPostfix]
        public static void NewAchievement(AchievementRuntime __instance)
        {
            GameData.checkLocation(__instance.OnlyId, "Achievement");
            GameData.CheckedAchievements.Add(__instance.OnlyId);
        }

        [HarmonyPatch(typeof(GameMapMgr), "GameFinish")]
        [HarmonyPrefix]
        public static bool GameFinish(GameMapMgr __instance, bool isWin)
        {
            if (isWin)
            {
                if (GameData.Difficulty > GameData.MinDifficulty &&
                    GameData.MaxStages.GetValueOrDefault<CharacterID, int>(GameData.Character) < 4)
                {
                    if (GameData.MinDifficulty < GameData.MaxScalingDifficulty) GameData.MinDifficulty++;
                    GameData.checkLocation(GameData.Character, "Character", 4);
                    GameData.MaxStages[GameData.Character] = 4;
                }
                if (GameData.Difficulty > GameData.HighestDifficulty)
                {
                    GameData.HighestDifficulty = GameData.Difficulty;
                    GameData.checkLocation(GameData.Difficulty, "Difficulty");
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(MapDataManager), "NextSection")]
        [HarmonyPrefix]
        public static bool ClearSection(MapDataManager __instance)
        {
            var section = __instance.CurSectionNo;
            if (GameData.Difficulty > GameData.MinDifficulty &&
                    GameData.MaxStages.GetValueOrDefault<CharacterID, int>(GameData.Character) < section)
            {
                GameData.checkLocation(GameData.Character, "Character", section);
                GameData.MaxStages[GameData.Character] = section;
            }
            return true;
        }
    }

    class GameSetupPatches
    {        
        [HarmonyPatch(typeof(AccountGameDataHandle), "InitGameData")]
        [HarmonyPrefix]
        public static bool setUpGameData()
        {
            GameData.setUpGameData();
            return true;
        }
        //[HarmonyPatch(typeof(MaJiang.GameMap.GameMapMgr), "StartGameLog")]
        [HarmonyPatch(typeof(MaJiang.GameMap.GameMapMgr), "GenNewMap")]
        [HarmonyPostfix]
        public static void onNewGame()
        {
            GameData.enterGame();

            // Debug
            //ReversePatches.UnlockLingYong(GameData.SaverInstance,
            //    new[] { XiaoChou.Bao4ShiXiaoYao, XiaoChou.JuCaiHe, XiaoChou.HuPengGui });
            //ReversePatches.UnlockRelic(GameData.SaverInstance,
            //    new[] { RelicId.ChuXuGuan });
            //ReversePatches.TryAddCharacter(AccountGameDataHandle.Instance ,CharacterID.TanCaiJiangShi);
        }
        [HarmonyPatch(typeof(MaJiang.GameMap.GameMapMgr), "ContinueGame")]
        [HarmonyPostfix]
        public static void onContinueGame()
        {
            GameData.enterGame();
        }
        [HarmonyPatch(typeof(GameMapMgr), "DoBattleOverFlow")]
        [HarmonyPrefix]
        public static bool BattleOver()
        {
            GameData.InBattle = false;
            return true;
        }
        [HarmonyPatch(typeof(GameMapMgr), "EnterFight")]
        [HarmonyPrefix]
        public static bool BattleStart()
        {
            GameData.InBattle = true;
            return true;
        }
        [HarmonyPatch(typeof(GameManager), "SaveGameAsync")]
        [HarmonyPostfix]
        public static void SaveGame()
        {
            GameData.SaveAsync();
        }
    }
    class UIPatches
    {
        [HarmonyPatch(typeof(MaJiang.OptionMenuPanelCtrl), "OnEnable")]
        [HarmonyPrefix]
        public static bool addArchipelagoSettingButton(MaJiang.OptionMenuPanelCtrl __instance)
        {
            ArchipelagoUI.AddArchipelagoSettingButton(__instance);

            return true;
        }
    }

class DebugPatches
{
    //private static int gotDict = 0;
    private static bool gotNames = false;
    private static bool gotAchievements = false;
    private static Dictionary<string, string[]> fullDict = new Dictionary<string, string[]>();
    
    /*[HarmonyPatch(typeof(I2.Loc.LocalizationManager), "GetTranslation")]
    [HarmonyPostfix]
    public static void checkTranslation(ref string Term, string __result)
    {
        if (Term.EndsWith("Name"))
        {
           ArchipelagoPlugin.Log.LogInfo($"{Term} translates to {__result}");
        }
    }*/
        /*[HarmonyPatch(typeof(I2.Loc.LanguageSourceData), "GetTermData")]
        [HarmonyPostfix]
        public static void getTermDictionary(I2.Loc.LanguageSourceData __instance)
        {
            if (gotDict == 10000 && gotNames)
            {
                //I2.Loc.LanguageSourceData castInstance = (I2.Loc.LanguageSourceData)__instance;
                //gotDict = true;
                //System.IO.File.WriteAllText("term dict.json", System.Text.Json.JsonSerializer.Serialize(
                //    __instance.mDictionary));
                //System.IO.File.WriteAllLines("term dict.txt",
                //    __instance.mDictionary.)
                //using (System.IO.StreamWriter file = new System.IO.StreamWriter("term dict.txt"))
                //    foreach (var entry in __instance.mDictionary)
                //    {
                //        file.WriteLine($"{entry.key}: {entry.value.GetTranslation(0)} | {entry.value.GetTranslation(1)}");
                //    }
                /*
                foreach (var id in Enum.GetValues<RelicId>())
                {
                    string key = $"Relic {id.GetName()}";
                    if (!fullDict.ContainsKey(key))
                    {
                        string[] names = { RelicIdExtend.GetName(id) };
                        fullDict.Add(key, names);
                    }
                }
                *|/
                System.IO.File.WriteAllText("term dict.json", System.Text.Json.JsonSerializer.Serialize(fullDict));
               ArchipelagoPlugin.Log.LogInfo("dict written to file");
            }
            else if (gotDict % 100 == 0 && gotDict < 10000)
            {
                foreach (var entry in __instance.mDictionary)
                {
                    if (!fullDict.ContainsKey(entry.key))
                    {
                        string[] translations = { entry.value.GetTranslation(0), entry.value.GetTranslation(1) };
                        fullDict.Add(entry.key, translations);
                    }
                }
            }
            else if (!gotNames && gotDict == 10000) gotDict--;
            gotDict++;
        }*/
        [HarmonyPatch(typeof(GlobalStaticDataManager), "get_AllRelicInGame")]
        [HarmonyPostfix]
        public static void getFigurineRelicNames(GlobalStaticDataManager __instance)
        // Expand to include names of more things
        {
            if (!gotNames)
            {
                gotNames = true;
                var figurineList = __instance.XiaoChouPaiPayloadTotalList;
                for (int i = 0; i < figurineList.Count; i++)
                {
                    string key = $"XiaoChou.{Enum.GetName(typeof(XiaoChou), figurineList[i].ID)}";
                    if (!fullDict.ContainsKey(key))
                    {
                        string[] names = { figurineList[i].displayName };
                        fullDict.Add(key, names);
                    }
                }
                var relicList = __instance.RelicDisplayTotalList;
                for (int i = 0; i < relicList.Count; i++)
                {
                    string key = $"RelicId.{Enum.GetName(typeof(RelicId), relicList[i].ID)}";
                    if (!fullDict.ContainsKey(key))
                    {
                        string[] names = { relicList[i].displayName };
                        fullDict.Add(key, names);
                    }
                }
                var yakuList = __instance.FanZhongPayloadList;
                for (int i = 0; i < yakuList.Count; i++)
                {
                    string key = $"FanZhong.{Enum.GetName(typeof(FanZhong), yakuList[i].ID)}";
                    if (!fullDict.ContainsKey(key))
                    {
                        string[] names = { yakuList[i].displayName };
                        fullDict.Add(key, names);
                    }
                }
                var offeringList = __instance.OfferingTotalList;
                for (int i = 0; i < offeringList.Count; i++)
                {
                    string key = $"Offering.{Enum.GetName(typeof(Offering), offeringList[i].ID)}";
                    if (!fullDict.ContainsKey(key))
                    {
                        string[] names = { offeringList[i].displayName };
                        fullDict.Add(key, names);
                    }
                }

                System.IO.File.WriteAllText("term names.json", System.Text.Json.JsonSerializer.Serialize(fullDict));
                ArchipelagoPlugin.BepinLogger.LogInfo("dict written to file");
            }
        }
        [HarmonyPatch(typeof(AchievementManager), "AllUnlockedAchievementCheck")]
        [HarmonyPostfix]
        public static void getAchievmentNames(AchievementManager __instance)
        {
            if (!gotAchievements)
            {
                gotAchievements = true;
                var achievementList = __instance.RuntimeAchievements;
                Dictionary<string, string> achievementDict = new Dictionary<string, string>();
                for (int i = 0; i < achievementList.Count; i++)
                {
                    string key = achievementList[i].OnlyId;
                    if (!achievementDict.ContainsKey(key))
                    {
                        achievementDict.Add(key, achievementList[i].Payload.displayNameTerm.ToString());
                    }
                }
                System.IO.File.WriteAllText("achievements.json", System.Text.Json.JsonSerializer.Serialize(achievementDict));
                ArchipelagoPlugin.BepinLogger.LogInfo("achievements written to file");
            }
        }
        [HarmonyPatch(typeof(RelicPanelCtr), "OnEnable")]
        [HarmonyPostfix]
        public static void RelicPanelOnEnable(RelicPanelCtr __instance)
        {
            var logger = ArchipelagoPlugin.BepinLogger;
            var list = __instance._unlockedRelicList;
            logger.LogInfo("Unlocked Relic List:");
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {list[i]}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("Available Relic List:");
            list = __instance._availableRelicList;
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {list[i]}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("NeedUnlocked Relic List:");
            var list2 = __instance._relicNeedUnLockedList;
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {list2[i]}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("Mysterious Relic List:");
            var list3 = __instance._relicMysteriousList;
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {list3[i]}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("Saver Unlocked Relic List:");
            var saver = __instance._saver;
            list = saver.UnlockedRelicList;
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {list[i]}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            var staticDataManager = MaJiang.GlobalDataCenter.Instance.staticDataMgr;
            logger.LogInfo("Static Data Manager Relic Display List:");
            var listStatic2 = staticDataManager._relicDisplayList;
            for (int i = 0; ; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {listStatic2[i].displayId}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("Static Data Manager NeedUnlocked Relic List:");
            var listStatic3 = staticDataManager.RelicNeedUnLockedList;
            for (int i = 0; ; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {((RelicId)listStatic3[i]).ToString()}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("Static Data Manager Mysterious Relic List:");
            var listStatic4 = staticDataManager.RelicDisplayMysteriousTotalList;
            for (int i = 0; ; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {listStatic4[i].displayId}");
                }
                catch (Exception)
                {
                    break;
                }
            }
            logger.LogInfo("GlobalDataCenter Available Relic Total List:");
            var listGlobal4 = new Il2CppSystem.Collections.Generic.List<RelicDisplay>(GlobalDataCenter.Instance.AvailableRelicTotalList);
            for (int i = 0; ; i++)
            {
                try
                {
                    logger.LogInfo($"{i}: {listGlobal4[i].displayId}");
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
    }
}
