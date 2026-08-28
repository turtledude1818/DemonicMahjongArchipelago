using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using MaJiang;
using MaJiang.Achievement;
using MaJiang.Achievement.Runtime;
using MaJiang.DataConstruct;
using MaJiang.DataConstruct.BaoLingPai;
using MaJiang.DataConstruct.Character;
using MaJiang.DataConstruct.GameEvent;
using MaJiang.DataConstruct.MaJiang;
using MaJiang.DataConstruct.Offering;
using MaJiang.DataConstruct.Relic;
using MaJiang.DataConstruct.XiaoChouPai;
using MaJiang.DLC;
using MaJiang.GameEvent.Runtime.Controller;
using MaJiang.GameEvent.Shop;
using MaJiang.GameMap;
using MaJiang.GM;
using MaJiang.Log;
using MaJiang.PlayMaJiang.Player.Relic;
using MaJiang.PlayMaJiang.UI.Bag;
using MaJiang.PlayMaJiang.UI.Illustration;
using MaJiang.UI;
using MaJiang.UI.GameEvent.Shop;
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
                ArchipelagoPlugin.BepinLogger.LogMessage($"Blocking unlock of relic {id}");
            }
            return false;
        }
        [HarmonyPatch(typeof(MaJiang.GM.Saver), "UnlockLingYong", new Type[] { typeof(XiaoChou[]) })]
        [HarmonyPrefix]
        static bool blockLingYongUnlock(Saver __instance, XiaoChou[] lingYongIds)
        {
            foreach (XiaoChou id in lingYongIds)
            {
                ArchipelagoPlugin.BepinLogger.LogMessage($"Blocking unlock of figurine {id}");
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
            ArchipelagoPlugin.BepinLogger.LogMessage($"Blocking unlock of character {characterID}");
            return false;
        }
        [HarmonyPatch(typeof(AchievementRuntime), "TryUnlockProp")]
        [HarmonyPrefix]
        static bool blockAchievementPropUnlock(AchievementRuntime __instance)
        {
            ArchipelagoPlugin.BepinLogger.LogMessage($"Blocking unlock from achievement {__instance.Name}");
            return false;
        }
        // Fix shop giving locked items
        [HarmonyPatch(typeof(ShopExtend), "TryGetBackupList")]
        [HarmonyPrefix]
        static bool blockBackupList() //sic
        {
            return false;
        }
    }

    class OverridePatches
    {
        private static GameRewardPanelCtrl _rewardPanel;
        private static RewardsType _rewardType;

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
            var unlocked = GameData.ReceivedCharacters;
            var showIndex = __instance.characterShowIndex;
            var itemDict = __instance.characterItemDict;
            var saveData = AccountGameDataHandle.Instance.GetPlayerCharcterSaveDatas();
        
            // Selecting unlocked chars
            __instance.unLockList.Clear();
            foreach (CharacterID id in unlocked)
            {
                __instance.unLockList.Add(id);
                itemDict[id].SetUnlock(true);
            }
        
        
            // Reimplementation
            foreach (var data in saveData)
            {
                var id = data.CharacterID;
                if (unlocked.Contains(id)) itemDict[id].SetSaveData(data);
            }
        
            __instance.countUnlockCharacter.text = unlocked.Count.ToString();
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

        [HarmonyPatch(typeof(GlobalDataCenter), "get_AvailableRelicTotalList")]
        [HarmonyPrefix]
        public static bool AvailableRelicTotalList(
            ref object __result)
        {
            var list = new Il2CppSystem.Collections.Generic.List<RelicDisplay>();
            var totallist = GlobalDataCenter.Instance.staticDataMgr._relicDisplayList;
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

        [HarmonyPatch(typeof(GlobalDataCenter), "get_AvailableXiaoChouTotalList")]
        [HarmonyPrefix]
        public static bool AvailableXiaoChouTotalList(
            ref object __result)
        {
            var list = new Il2CppSystem.Collections.Generic.List<XiaoChouPaiPayload>();
            var totallist = GlobalDataCenter.Instance.staticDataMgr._xiaoChouPaiPayloads;
            for (int i = 0; i < totallist.Count; i++)
            {
                XiaoChou id = totallist[i].id;
                //if (GameData.startingRelics.Concat(GameData.receivedRelics).Contains(id))
                if (GameData.ReceivedFigurines.Contains(id))
                {
                    list.Add(totallist[i]);
                }
            }
            __result = list;
            return false;
        }

        // These two patches should fix getting locked figurines after battles
        [HarmonyPatch(typeof(GameRewardPanelCtrl), "OnRewardBtnClick")]
        [HarmonyPrefix]
        public static bool GetGameRewardType(GameRewardPanelCtrl __instance, RewardsType rewardsType)
        {
            _rewardPanel = __instance;
            _rewardType = rewardsType;
            return true;
        }
        [HarmonyPatch(typeof(GameMapMgr), "PopNodeUI")]
        [HarmonyPrefix]
        public static bool replaceGameRewardEvent(GameEvent gameEvent)
        {
            if (_rewardType == RewardsType.LingYong && gameEvent != null)
            {
                gameEvent = _rewardPanel.lingyongRewardsEvent;
            }
            return true;
        }
        // Fix shop giving locked relics
        [HarmonyPatch(typeof(ShopExtend), "GetTotalList")]
        [HarmonyPostfix]
        public static void changeTotalListToUnlockedOnly(NodeShop _shop, ItemType _typeEnum, IItemList __result)
        {
            if (_typeEnum != ItemType.Relic) { return; }
            var list = new Il2CppSystem.Collections.Generic.List<RelicDisplay>();
            var array = __result.Cast<ArrayItemList<RelicDisplay>>();
            for (int i = 0; i < __result.ItemList.Count(); i++)
            {
                var relic = (RelicDisplay)array._array[i];
                if (GameData.IsItemUnlocked((RelicId)relic.ID))
                {
                    list.Add(relic);
                }
            }
            array._array = list.ToArray();
        }

        //[HarmonyPatch(typeof(NodeShop), "GetRandom")]
        //[HarmonyPrefix]
        //public static bool ShopGetRandom(ref Il2CppSystem.Collections.Generic.IReadOnlyList<IItemInfo> _exclusive)
        //{
        //    var list = _exclusive.Cast<Il2CppSystem.Collections.Generic.List<IItemInfo>>();
        //    var allRelics = GlobalDataCenter.Instance.AllRelicInGame.ToList();
        //    foreach (var relic in allRelics)
        //    {
        //        if (!GameData.IsItemUnlocked((RelicId)relic.ID)) {
        //            list.Add(relic.Cast<IItemInfo>());
        //        }
        //    }
        //    return true;
        //}
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
        //[HarmonyReversePatch]
        //[HarmonyPatch(typeof(Saver), "UnlockRelic", new Type[] {typeof(Il2CppStructArray<RelicId>) })]
        //public static void UnlockRelic(object __instance, Il2CppStructArray<RelicId> relicIds)
        //{
        //    throw new NotImplementedException();
        //}
        //[HarmonyReversePatch]
        //[HarmonyPatch(typeof(Saver), "UnlockLingYong", new Type[] { typeof(Il2CppStructArray<XiaoChou>) })]
        //public static void UnlockLingYong(object __instance, Il2CppStructArray<XiaoChou> lingYongIds)
        //{
        //    throw new NotImplementedException();
        //}
        //[HarmonyReversePatch]
        //[HarmonyPatch(typeof(AccountGameDataHandle), "TryAddCharacter")]
        //public static bool TryAddCharacter(object __instance, CharacterID characterId, bool writeSave = true)
        //{
        //    throw new NotImplementedException();
        //}
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(PopUIManager), "OpenUI", typeof(GameObject), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
        public static IManagedUI OpenUI(PopUIManager __instance, GameObject prefab, Il2CppReferenceArray<Il2CppSystem.Object> openParams = null)
        {
            throw new NotImplementedException();
        }
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ShopExtend), "GetTotalList")]
        public static IItemList GetTotalList(NodeShop _shop, ItemType _typeEnum)
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
        // Change to override to fix unlocking items after DLC update
        //[HarmonyPatch(typeof(Saver), "PlayFanNewFoundAdd")]
        //[HarmonyPrefix]
        //public static bool NewYaku(FanZhong fanZhong, Saver __instance)
        //{
        //    GameData.checkLocation(fanZhong, "Yaku");
        //    GameData.CheckedYaku.Add(fanZhong);
        //
        //    __instance._playFanNewFound.Add(fanZhong);
        //
        //    return false;
        //}

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
                if (GameData.Difficulty >= GameData.MinDifficulty &&
                    GameData.MaxStages.GetValueOrDefault<CharacterID, int>(GameData.Character) < 4)
                {
                    if (GameData.MinDifficulty < GameData.MaxScalingDifficulty) GameData.MinDifficulty++;
                    GameData.checkLocation(GameData.Character, "Character", 4);
                    GameData.MaxStages[GameData.Character] = 4;
                    ArchipelagoPlugin.BepinLogger.LogInfo($"Cleared Game as {GameData.Character}");
                }
                if (GameData.Difficulty > GameData.HighestDifficulty)
                {
                    GameData.HighestDifficulty = GameData.Difficulty;
                    GameData.checkLocation(GameData.Difficulty, "Difficulty");
                }
            }
            return true;
        }

        //[HarmonyPatch(typeof(MapDataManager), "NextLevel")]
        //[HarmonyPrefix]
        //public static bool ClearLevel(MapDataManager __instance)
        //{
        //    ArchipelagoPlugin.BepinLogger.LogInfo("Going to Next Level");
        //    var level = __instance.CurLevelNo;
        //    if (GameData.Difficulty >= GameData.MinDifficulty &&
        //            GameData.MaxStages.GetValueOrDefault<CharacterID, int>(GameData.Character) < level)
        //    {
        //        GameData.checkLocation(GameData.Character, "Character", level);
        //        GameData.MaxStages[GameData.Character] = level;
        //    }
        //    return true;
        //}
        [HarmonyPatch(typeof(GameMapMgr), "CheckToFinalChapter")]
        [HarmonyPrefix]
        public static bool CheckLevelWin(GameMapMgr __instance)
        {
            var level = __instance.mapDataMgr.CurLevelNo;
            var section = __instance.mapDataMgr.CurSectionNo;
            if (section != 3) return true;
            
            if (GameData.Difficulty >= GameData.MinDifficulty &&
                    GameData.MaxStages.GetValueOrDefault<CharacterID, int>(GameData.Character) < level)
            {
                GameData.checkLocation(GameData.Character, "Character", level);
                GameData.MaxStages[GameData.Character] = level;
            }
            return true;
        }

        [HarmonyPatch(typeof(OrderButton), "Buy")]
        [HarmonyPrefix]
        public static bool CheckMilkTea(OrderButton __instance)
        {
            // Not implemented
            //GameData.checkLocation(__instance.OnlyId, "Milk Tea");
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
        [HarmonyPatch(typeof(GameMapMgr), "ContinueGame")]
        [HarmonyPostfix]
        public static void onContinueGame()
        {
            GameData.enterGame();
        }
        [HarmonyPatch(typeof(GameMapMgr), "BackToHome")]
        [HarmonyPostfix]
        public static void onBackToHome()
        {
            GameData.InGame = false;
        }
        [HarmonyPatch(typeof(GameMapMgr), "DoBattleOverFlow")]
        [HarmonyPrefix]
        public static bool BattleOver(GameMapMgr __instance)
        {
            GameData.InBattle = false;

            var level = __instance.mapDataMgr.CurLevelNo;
            var section = __instance.mapDataMgr.CurSectionNo;
            if (section == 3)
            {
                ArchipelagoPlugin.BepinLogger.LogInfo($"Cleared Level {level} as {GameData.Character}");
                if (GameData.Difficulty >= GameData.MinDifficulty &&
                        GameData.MaxStages.GetValueOrDefault<CharacterID, int>(GameData.Character) < level)
                {
                    GameData.checkLocation(GameData.Character, "Character", level);
                    GameData.MaxStages[GameData.Character] = level;
                }
            }
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
            if (ArchipelagoClient.Authenticated) GameData.SaveAsync();
        }
        [HarmonyPatch(typeof(GameManager), "OnApplicationQuit")]
        [HarmonyPrefix]
        public static bool OnQuit()
        {
            Task.Run(async () => await GameData.SaveAsync())
                .GetAwaiter()
                .GetResult();
            return true;
        }
        [HarmonyPatch(typeof(GameMapMgr), "OnLoadMapFinish")]
        [HarmonyPostfix]
        public static void OnLoadMap()
        {
            GameData.InBattle = false;
            GameData.InGame = true;
            GameData.processUnprocessed();
        }
    }
    class UIPatches
    {
        [HarmonyPatch(typeof(OptionMenuPanelCtrl), "OnEnable")]
        [HarmonyPrefix]
        public static bool AddArchipelagoSettingButton(OptionMenuPanelCtrl __instance)
        {
            ArchipelagoUI.AddArchipelagoSettingButton(__instance);

            return true;
        }
        [HarmonyPatch(typeof(HomePanelCtrl), "OnNewGameBtnClick")]
        [HarmonyPrefix]
        public static bool BlockNewGame()
        {
            if (!ArchipelagoClient.Authenticated)
            {
                ArchipelagoUI.CreateInfoPanel("Archipelago Not Connected",
                    "Connect to Archipelago server before starting a new game, " +
                    "or remove the mod to play without Archipelago.");
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(HomePanelCtrl), "OnContinueBtnClick")]
        [HarmonyPrefix]
        public static bool BlockContinue()
        {
            if (!ArchipelagoClient.Authenticated)
            {
                ArchipelagoUI.CreateInfoPanel("Archipelago Not Connected",
                    "Connect to Archipelago server before continuing a game, " +
                    "or remove the mod to play without Archipelago.");
                return false;
            }
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
        //[HarmonyPatch(typeof(AchievementManager), "AllUnlockedAchievementCheck")]
        //[HarmonyPostfix]
        //public static void getAchievmentNames(AchievementManager __instance)
        //{
        //    if (!gotAchievements)
        //    {
        //        gotAchievements = true;
        //        var achievementList = __instance.RuntimeAchievements;
        //        Dictionary<string, string> achievementDict = new Dictionary<string, string>();
        //        for (int i = 0; i < achievementList.Count; i++)
        //        {
        //            string key = achievementList[i].OnlyId;
        //            if (!achievementDict.ContainsKey(key))
        //            {
        //                achievementDict.Add(key, achievementList[i].Payload.displayNameTerm.ToString());
        //            }
        //        }
        //        System.IO.File.WriteAllText("achievements.json", System.Text.Json.JsonSerializer.Serialize(achievementDict));
        //        ArchipelagoPlugin.BepinLogger.LogInfo("achievements written to file");
        //    }
        //}
    }
}
