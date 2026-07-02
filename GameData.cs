using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using I2.Loc.SimpleJSON;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MaJiang;
using MaJiang.Achievement;
using MaJiang.Achievement.Runtime;
using MaJiang.DataConstruct;
using MaJiang.DataConstruct.Achievement;
using MaJiang.DataConstruct.Character;
using MaJiang.DataConstruct.MaJiang;
using MaJiang.DataConstruct.Offering;
using MaJiang.DataConstruct.Relic;
using MaJiang.DataConstruct.XiaoChouPai;
using MaJiang.GameMap;
using MaJiang.GM;
using MaJiang.PlayMaJiang.Player.Offering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace DemonicMahjongArchipelago
{
    internal class GameData
    {
        private static ArchipelagoClient Client;

        private static ManualLogSource BepinLogger = ArchipelagoPlugin.BepinLogger;

        // Game Data Management
        public static GameManager GameManagerInstance;
        public static Saver SaverInstance;
        public static bool InGame = false;
        public static bool InBattle = false;
        public static int Difficulty;
        public static int MinDifficulty;
        public static int MaxScalingDifficulty;
        public static CharacterID Character;
        private static SaveData _saveData;
        private static string _savePath;
        internal static string Seed;
        public static int LastProcessedItem = -1;
        //public static List<(object, Type)> unProcessedItems = new List<(object item, Type type)>();
        public static Queue<ItemInfo> UnprocessedItems = new Queue<ItemInfo>();
        private static Queue<ItemInfo> _failedItems = new Queue<ItemInfo>();

        // Items
        public static HashSet<RelicId> ReceivedRelics = new HashSet<RelicId>();
        public static HashSet<XiaoChou> ReceivedFigurines = new HashSet<XiaoChou>();
        public static HashSet<CharacterID> ReceivedCharacters = new HashSet<CharacterID>();
        // Debug
        //public static HashSet<RelicId> ReceivedRelics = new HashSet<RelicId> { RelicId.ChuXuGuan };
        //public static HashSet<XiaoChou> ReceivedFigurines = new HashSet<XiaoChou> { XiaoChou.Bao4ShiXiaoYao, XiaoChou.JuCaiHe, XiaoChou.HuPengGui };
        //public static HashSet<CharacterID> ReceivedCharacters = new HashSet<CharacterID> { CharacterID.TanCaiJiangShi };

        // Locations
        public static HashSet<FanZhong> CheckedYaku = [];
        public static Dictionary<CharacterID, int> MaxStages = new Dictionary<CharacterID, int>();
        public static HashSet<string> CheckedAchievements = new HashSet<string>();
        public static int HighestDifficulty = -1;


        public static bool ConnectSetupComplete = false;
        private static bool _saving = false;

        public static void checkLocation(object location, string type, int misc = 0)
        {
            int id = 0;
            switch (type)
            {
                case "Yaku":
                    id = LocationNames.YakuToId[(FanZhong)location] + LocationNames.YAKU_OFFSET + 1;
                    break;
                case "Achievement":
                    id = LocationNames.AchievementToId[(string)location] + LocationNames.ACHIEVEMENT_OFFSET + 1;
                    break;
                case "Difficulty":
                    id = (int)location + LocationNames.DIFFICULTY_OFFSET + 1;
                    break;
                case "Character":
                    if (misc == 0) break;
                    id = ItemNames.CharacterToId[(CharacterID)location] + ((misc - 1) * 30)
                        + LocationNames.CHARACTER_OFFSET + 1;
                    break;
                default:
                    break;
            }
            if (id == 0) throw new NotImplementedException($"{type} is not a valid location type");
            Client.checkLocation(id);
        }

        public static void checkAllLocations()
        {
            //Yaku
            var allYaku = SaverInstance.PlayPlayFanNewFound.Cast<Il2CppSystem.Collections.Generic.List<FanZhong>>();
            foreach (var yaku in allYaku)
            {
                checkLocation(yaku, "Yaku");
            }
            //Achievements
            var allAchievements = SaverInstance.CurAchievementGetList.
                Cast<Il2CppSystem.Collections.Generic.List<AchievementSaveData>>();
            foreach(var achievement in allAchievements)
            {
                checkLocation(achievement.guid, "Achievement");
            }
            //Character Full Clear
            var allCharacterData = SaverInstance.CharacterSaveDataList
                .Cast<Il2CppSystem.Collections.Generic.List<CharacterSaveData>>();
            foreach (var character in allCharacterData)
            {
                var difficulty = character.highestDifficulty;
                // Uncleared characters have difficulty as 0
                if (difficulty > 0 && difficulty > MinDifficulty)
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        checkLocation(character.CharacterID, "Character", i);
                    }
                }
            }

        }

        public static void processUnprocessed()
        {
            while (UnprocessedItems.Count > 0)
            {
                receiveItem(UnprocessedItems.Dequeue(), true);
            }
            UnprocessedItems = _failedItems;
            _failedItems = new Queue<ItemInfo>();
        }

        public static void receiveItem(ItemInfo item, bool fromUnprocessed = false)
        {
            if (item == null) throw new ArgumentNullException("item");
            int id = (int)item.ItemId;
            var queue = fromUnprocessed ? _failedItems : UnprocessedItems;

            if (!fromUnprocessed) BepinLogger.LogMessage($"Received {item.ItemName}");
            if (!ConnectSetupComplete)
            {
                BepinLogger.LogInfo($"Not finished connecting, item {item.ItemName} not received");
                queue.Enqueue(item);
                return;
            }
            if (!InGame)
            {
                if (id >= ItemNames.FILLER_OFFSET)
                {
                    queue.Enqueue(item);
                    return;
                }
            }
            if (id < ItemNames.FIGURINE_OFFSET)
            {
                var character = ItemNames.CharacterIds[id - ItemNames.CHAR_OFFSET - 1];
                ReceivedCharacters.Add(ItemNames.CharacterIds[id - ItemNames.CHAR_OFFSET - 1]);
                //ReversePatches.TryAddCharacter(AccountGameDataHandle.Instance, character, true);
                UnlockItem(character, typeof(CharacterID));
            }
            else if (id < ItemNames.RELIC_OFFSET)
            {
                var figurine = ItemNames.FigurineIds[id - ItemNames.FIGURINE_OFFSET - 1];
                ReceivedFigurines.Add(figurine);
                //ReversePatches.UnlockLingYong(GameManager.Instance.Saver, new[] { figurine });
                UnlockItem(figurine, typeof(XiaoChou));
            }
            else if (id < ItemNames.FILLER_OFFSET)
            {
                var relic = ItemNames.RelicIds[id - ItemNames.RELIC_OFFSET - 1];
                ReceivedRelics.Add(relic);
                //ReversePatches.UnlockRelic(GameManager.Instance.Saver, new[] { relic });
                UnlockItem(relic, typeof(RelicId));
            }
            else
            {
                return;
                var name = item.ItemName;
                if (name == null) throw new ArgumentNullException("item.name", "Item name could not be resolved");

                if (name[..name.IndexOf(' ')] == "Lose")
                {
                    BepinLogger.LogWarning("Haven't implemented trap items");
                }

                var type = name[(name.LastIndexOf(' ')+1)..];
                if (!int.TryParse(name[..(name.IndexOf(' '))], out int value))
                {
                    throw new NotImplementedException($"{name} is not a valid item");
                }
                switch (type)
                {
                    case "Gold":
                        GlobalDataCenter.Instance.SetData(GlobalDataType.Coins,
                            GlobalDataCenter.Instance.GetData<int>(GlobalDataType.Coins) + value, null);
                        break;
                    case "Energy":
                        GlobalDataCenter.Instance.SetData(GlobalDataType.Soul,
                            GlobalDataCenter.Instance.GetData<int>(GlobalDataType.Soul) + value, null);
                        break;
                    case "HP:":
                        GlobalDataCenter.Instance.SetData(GlobalDataType.Hp,
                            GlobalDataCenter.Instance.GetData<int>(GlobalDataType.Hp) + value, null);
                        break;
                    default:
                        throw new NotImplementedException($"{name} is not a valid item");
                }
            }
        }
        // Have to reimplement rather than call game functions because of AccessViolationExceptions
        public static void UnlockItem(object item, Type type)
        {
            if (type == typeof(CharacterID))
            {
                var characterId = (CharacterID)item;
                var __instance = AccountGameDataHandle.Instance;

                var saver = GameManager.Instance.Saver;
                if (saver._freeModeUnLockCharacterList.Contains(characterId)) return;
                saver._freeModeUnLockCharacterList.Add(characterId);

                // Reimplement AddCharacterSaverData(characterId, writeSave)
                var saveData = new CharacterSaveData(characterId);
                saveData.highestDifficulty = 0;
                var roleSet = __instance.gameData.achievements.ownedRoleSet;
                roleSet.AddItem(saveData);
            }
            else if (type == typeof(XiaoChou))
            {
                var lingYongId = (XiaoChou)item;
                var __instance = SaverInstance;

                if (__instance._unlockedLingYongList.Contains(lingYongId)) return;
                __instance._unlockedLingYongList.Add(lingYongId);
                var curr = __instance.CurrentUnlockedLingYongList.Cast<Il2CppSystem.Collections.Generic.List<int>>();
                curr.Add(((int)lingYongId));
                __instance.CurrentUnlockedLingYongList =
                    curr.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<int>>();
            }
            else if (type == typeof(RelicId))
            {
                var relicId = ((RelicId)item).ToString();
                var __instance = SaverInstance;

                if (__instance._unlockedRelicList.Contains(relicId)) return;
                __instance._unlockedRelicList.Add(relicId);
                var curr = __instance.CurrentUnlockedRelicList.Cast<Il2CppSystem.Collections.Generic.List<string>>();
                curr.Add(relicId);
                __instance.CurrentUnlockedRelicList =
                    curr.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>();
            }
            else return;
        }

        public static void setUpGameData()
        {
            GameManagerInstance = MaJiang.GM.GameManager.Instance;
            SaverInstance = GameManagerInstance.Saver;
            var index = GameManagerInstance.GameSaverUtil._saverPath.LastIndexOf('/');
            _savePath = GameManagerInstance.GameSaverUtil._saverPath[..index];

            //#if DEBUG
            //            var all = "\nYaku: \n";
            //            for (int i = 0; i < GlobalDataCenter.Instance.staticDataMgr.FanZhongPayloadList.Count; i++)
            //            {
            //                var fanZhong = GlobalDataCenter.Instance.staticDataMgr.FanZhongPayloadList[i];
            //                int rarity;
            //                var fan = fanZhong.fan;
            //                if (fan <= 8) rarity = 1;
            //                else if (fan <= 16) rarity = 2;
            //                else if (fan <= 32) rarity = 3;
            //                else if (fan <= 64) rarity = 4;
            //                else if (fan <= 88) rarity = 5;
            //                else rarity = 6;
            //                all += $"\t\"{fanZhong.Name}\": {rarity},\n";
            //            }
            //            all += "Relics: \n" ;
            //            for (int i = 0; i < GlobalDataCenter.Instance.staticDataMgr.RelicDisplayTotalList.Count; i++)
            //            {
            //                var relic = GlobalDataCenter.Instance.staticDataMgr.RelicDisplayTotalList[i];
            //                all += $"\t\"{relic.Name}\": {relic.rarity},\n";
            //            }
            //            all += "Figurines: \n";
            //            for (int i = 0; i < GlobalDataCenter.Instance.staticDataMgr.XiaoChouPaiPayloadTotalList.Count; i++)
            //            {
            //                var figurine = GlobalDataCenter.Instance.staticDataMgr.XiaoChouPaiPayloadTotalList[i];
            //                all += $"\t\"{figurine.Name}\": {figurine.rarity},\n";
            //            }
            //            
            //            BepinLogger.LogInfo(all);
            //#endif
            //var all = "\nOffering: \n";
            ////var offeringList = GlobalDataCenter.Instance.staticDataMgr.OfferingDisplayTotalList;
            //var offeringList = Enum.GetValues<Offering>();
            //for (int i = 0; i < offeringList.Length; i++)
            //{
            //    var offering = GlobalDataCenter.Instance.GetOffering(offeringList[i]);
            //    if (offering == null) {
            //        BepinLogger.LogInfo($"could not get offering {offeringList[i]}");
            //        continue;
            //    };
            //    all += $"\t\t\t{{Offering.{offering.DisplayId}, \"{offering.Name}\"}},\n";
            //}
            //BepinLogger.LogInfo(all);
        }
        public static void enterGame()
        {
            InGame = true;
            Difficulty = MaJiang.Difficulty.DifficultyManager.Instance.CurDifficulty.index;
            Character = SaverInstance.LastUsedCharacterID;
            BepinLogger.LogInfo($"Entering game as {ItemNames.CharacterNames[Character]} on difficulty {Difficulty}");
            processUnprocessed();
        }

        public static CharacterID[] UnlockedChars()
        {
            return ReceivedCharacters.ToArray();
        }
        public static RelicId[] UnlockedRelics()
        {
            return ReceivedRelics.ToArray();
        }
        public static XiaoChou[] UnlockedFigurines()
        {
            return ReceivedFigurines.ToArray();
        }

        public static void ClearAllData()
        {
            SaverInstance.ClearUnlockLongYongList();
            SaverInstance.ClearCurrentUnlockLongYongList();
            SaverInstance.ClearUnlockRelicList();
            SaverInstance.ClearCurrentUnlockRelicList();
        }

        internal static void Init(ArchipelagoClient client)
        {
            Client = client;
        }

        public static async Task OnConnectSetup(LoginSuccessful success)
        {
            BepinLogger.LogInfo("Connection setup: loading save");
            await GameData.LoadSaveAsync();
            //if (ConnectSetupComplete) return;
            if (_saveData == null)
            {
                // Change game values (TODO: this will wipe character unlock progress, check when to apply)
                //SaverInstance._freeModeUnLockCharacterList = new Il2CppSystem.Collections.Generic.List<CharacterID>();
                //AccountGameDataHandle.Instance.freeModeCharacterIDs = SaverInstance._freeModeUnLockCharacterList
                //    .AsReadOnly().Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<CharacterID>>();
                //AccountGameDataHandle.Instance.gameData.achievements.ownedRoleSet =
                //    new Il2CppSystem.Collections.Generic.List<CharacterSaveData>()
                //    .ToArray().Cast<Il2CppReferenceArray<CharacterSaveData>>();


                // Get/Set Options
                Dictionary<string, object> options = success.SlotData;

                options.TryGetValue("character_min_difficulty", out object minDifficulty);
                if (minDifficulty != null) MinDifficulty = (int)((long)minDifficulty);

                options.TryGetValue("scaling_min_difficulty", out object scaling);
                if (scaling != null && (long)scaling != 0)
                {
                    MaxScalingDifficulty = MinDifficulty;
                    MinDifficulty = 0;
                }
            }
            BepinLogger.LogMessage("Connection setup completed");
            ArchipelagoClient.ServerData.Index = LastProcessedItem;
            ConnectSetupComplete = true;
            ArchipelagoPlugin.ArchipelagoClient.CheckItems();
        }

        internal class SaveData
        {
            public int LastProcessedItem { get; set; }
            public HashSet<RelicId> ReceivedRelics{ get; set; }
            public HashSet<XiaoChou> ReceivedFigurines{ get; set; }
            public HashSet<CharacterID> ReceivedCharacters{ get; set; }
            public HashSet<FanZhong> CheckedYaku{ get; set; }
            public Dictionary<CharacterID, int> MaxStages{ get; set; }
            public HashSet<string> CheckedAchievements{ get; set; }
            public int HighestDifficulty{ get; set; }
            public int MinDifficulty{ get; set; }
            public int MaxScalingDifficulty {  get; set; }
            public Queue<ItemInfo> UnprocessedItems{ get; set; }
            public Queue<ItemInfo> FailedItems{ get; set; }

            // Game save data
            //private Saver saver;


            public void LoadData()
            {
                GameData.LastProcessedItem = LastProcessedItem;
                GameData.ReceivedRelics = ReceivedRelics;
                GameData.ReceivedFigurines = ReceivedFigurines;
                GameData.ReceivedCharacters = ReceivedCharacters;
                GameData.CheckedYaku = CheckedYaku;
                GameData.MaxStages = MaxStages;
                GameData.CheckedAchievements = CheckedAchievements;
                GameData.HighestDifficulty = HighestDifficulty;
                GameData.MinDifficulty = MinDifficulty;
                GameData.MaxScalingDifficulty = MaxScalingDifficulty;
                GameData.UnprocessedItems = UnprocessedItems;
                GameData._failedItems = FailedItems;

                ArchipelagoClient.ServerData.Index = LastProcessedItem;

                //Modify game save data
                //GameData.SaverInstance = saver;
                //GameManager.Instance.Saver = saver;
            }
            public SaveData() {
                this.LastProcessedItem = GameData.LastProcessedItem;
                this.ReceivedRelics = GameData.ReceivedRelics;
                this.ReceivedFigurines= GameData.ReceivedFigurines;
                this.ReceivedCharacters= GameData.ReceivedCharacters;
                this.CheckedYaku = GameData.CheckedYaku;
                this.MaxStages = GameData.MaxStages;
                this.CheckedAchievements = GameData.CheckedAchievements;
                this.HighestDifficulty = GameData.HighestDifficulty;
                this.MinDifficulty = GameData.MinDifficulty;
                this.MaxScalingDifficulty = GameData.MaxScalingDifficulty;
                this.UnprocessedItems = GameData.UnprocessedItems;
                this.FailedItems = GameData._failedItems;

                //this._saver = GameData.SaverInstance;
            }
        }

        public static async Task SaveAsync()
        {
            if (_saving || Seed == null) return;
            try
            {
                _saving = true;
                ArchipelagoPlugin.BepinLogger.LogMessage("Saving Archipelago World");
                if (_savePath != null)
                {
                    Directory.CreateDirectory(_savePath);
                    var path = _savePath + $"/{Seed}.json";
                    _saveData = new SaveData();
                    if (_saveData == null)
                    {
                        BepinLogger.LogError("Couldn't create SaveData");
                        return;
                    }
                    BepinLogger.LogMessage($"Saving to {path}");

                    if (File.Exists(path))
                    {
                        BepinLogger.LogMessage("Backing up save");
                        var backupPath = path + ".bak";
                        await using FileStream source = File.OpenRead(path);
                        if (File.Exists(backupPath)) File.Delete(backupPath);
                        await using FileStream destination = File.Create(backupPath);
                        File.SetAttributes(backupPath, File.GetAttributes(backupPath) | FileAttributes.Hidden);
                        await source.CopyToAsync(destination);
                        await source.DisposeAsync();
                        await destination.DisposeAsync();
                        File.Delete(path);
                    }
                    await using FileStream createStream = File.Create(path);
                    try
                    {
                        await JsonSerializer.SerializeAsync(createStream, _saveData);
                        BepinLogger.LogMessage($"Data saved to {path}");
                    }
                    catch (Exception e)
                    {
                        BepinLogger.LogError($"Could not save file {path}");
                        BepinLogger.LogError(e);
                        _saveData = null;
                    }
                    await createStream.DisposeAsync();
                }
                else
                {
                    BepinLogger.LogWarning("Could not save data; Save path not found");
                }
            }
            catch(Exception e) { BepinLogger.LogError(e); }
            finally { _saving = false; }
        }

        public static async Task LoadSaveAsync()
        {
            // Add empty string to beginning in case _savePath is null
            var path = "" + _savePath + $"/{Seed}.json";
            if (_savePath != null && File.Exists(path))
            {
                using FileStream openStream = File.OpenRead(path);
                SaveData? data = await JsonSerializer.DeserializeAsync<SaveData>(openStream);
                if (data != null)
                {
                    _saveData = data;
                    _saveData.LoadData();
                    BepinLogger.LogMessage($"Loaded save data from file {path}");
                }
                else
                {
                    _saveData = null;
                    BepinLogger.LogError($"Could not read save data from file {path}");
                }
            }
            else if (_savePath == null) BepinLogger.LogWarning("Save path not found");
        }
    }
}
