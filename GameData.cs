using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;
using I2.Loc.SimpleJSON;
using MaJiang;
using MaJiang.Achievement.Runtime;
using MaJiang.DataConstruct.Character;
using MaJiang.DataConstruct.MaJiang;
using MaJiang.DataConstruct.Relic;
using MaJiang.DataConstruct.XiaoChouPai;
using MaJiang.GameMap;
using MaJiang.GM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace DemonicMahjongArchipelago
{
    internal class GameData
    {
        private static ArchipelagoClient Client;

        private static ManualLogSource BepinLogger = ArchipelagoPlugin.BepinLogger;

        // Game Data Management
        public static GlobalStaticDataManager GlobalStaticDataManagerInstance;
        public static GlobalDataCenter GlobalDataCenterInstance;
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
        public static List<ItemInfo> UnprocessedItems = new List<ItemInfo>();

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
        public static int HighestDifficulty = 0;

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

        public static void receiveItem(ItemInfo item, bool fromUnprocessed = false)
        {
            int id = (int)item.ItemId;
            if (!InGame)
            {
                if (id >= ItemNames.FILLER_OFFSET)
                {
                    if (!fromUnprocessed)
                    {
                        UnprocessedItems.Add(item);
                    }
                    return;
                }
            }
            if (id < ItemNames.FIGURINE_OFFSET)
            {
                var character = ItemNames.CharacterIds[id - ItemNames.CHAR_OFFSET - 1];
                ReceivedCharacters.Add(ItemNames.CharacterIds[id - ItemNames.CHAR_OFFSET - 1]);
                ReversePatches.TryAddCharacter(AccountGameDataHandle.Instance, character, true);
            }
            else if (id < ItemNames.RELIC_OFFSET)
            {
                var figurine = ItemNames.FigurineIds[id - ItemNames.FIGURINE_OFFSET - 1];
                ReceivedFigurines.Add(figurine);
                ReversePatches.UnlockLingYong(SaverInstance, new[] { figurine });
            }
            else if (id < ItemNames.FILLER_OFFSET)
            {
                var relic = ItemNames.RelicIds[id - ItemNames.RELIC_OFFSET - 1];
                ReceivedRelics.Add(relic);
                ReversePatches.UnlockRelic(SaverInstance, new[] { relic });
            }
            else
            {
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
                        GlobalDataCenterInstance.SetData(GlobalDataType.Coins,
                            GlobalDataCenterInstance.GetData<int>(GlobalDataType.Coins) + value, null);
                        break;
                    case "Energy":
                        GlobalDataCenterInstance.SetData(GlobalDataType.Soul,
                            GlobalDataCenterInstance.GetData<int>(GlobalDataType.Soul) + value, null);
                        break;
                    case "HP:":
                        GlobalDataCenterInstance.SetData(GlobalDataType.Hp,
                            GlobalDataCenterInstance.GetData<int>(GlobalDataType.Hp) + value, null);
                        break;
                    default:
                        throw new NotImplementedException($"{name} is not a valid item");
                }
            }
        }

        public static void setUpGameData()
        {
            GlobalDataCenterInstance = GlobalDataCenter.Instance;
            GlobalStaticDataManagerInstance = GlobalDataCenterInstance.staticDataMgr;
            GameManagerInstance = MaJiang.GM.GameManager.Instance;
            SaverInstance = GameManagerInstance.Saver;
            var index = GameManagerInstance.GameSaverUtil._saverPath.LastIndexOf('/');
            _savePath = GameManagerInstance.GameSaverUtil._saverPath[..index];

            // Change game values
            //SaverInstance._freeModeUnLockCharacterList = new Il2CppSystem.Collections.Generic.List<CharacterID>();
            //foreach(CharacterID character in ReceivedCharacters)
            //{
            //    SaverInstance._freeModeUnLockCharacterList.Add(character);
            //}
        }
        public static void enterGame()
        {
            InGame = true;
            Difficulty = MaJiang.Difficulty.DifficultyManager.Instance.CurDifficulty.index;
            Character = SaverInstance.LastUsedCharacterID;
            BepinLogger.LogInfo($"Entering game as {ItemNames.CharacterNames[Character]} on difficulty {Difficulty}");
            foreach (var item in UnprocessedItems)
            {
                receiveItem(item, true);
            }
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

        public static async Task onConnectSetup(LoginSuccessful success)
        {
            await GameData.LoadSaveAsync();
            if (_saveData == null)
            {
                // Get/Set Options
                Dictionary<string, object> options = new Dictionary<string, object>();

                options.TryGetValue("character_min_difficulty", out object minDifficulty);
                if (minDifficulty != null) MinDifficulty = (int)minDifficulty;

                options.TryGetValue("scaling_min_difficulty", out object scaling);
                if (scaling != null && (bool)scaling)
                {
                    MaxScalingDifficulty = MinDifficulty;
                    MinDifficulty = 0;
                }
            }
        }

        internal class SaveData : MonoBehaviour
        {
            private int _lastProcessedItem;
            private HashSet<RelicId> _receivedRelics;
            private HashSet<XiaoChou> _receivedFigurines;
            private HashSet<CharacterID> _receivedCharacters;
            private HashSet<FanZhong> _checkedYaku;
            private Dictionary<CharacterID, int> _maxStages;
            private HashSet<string> _checkedAchievements;
            private int _highestDifficulty;
            private int _minDifficulty;
            private List<ItemInfo> _unprocessedItems;

            public void LoadData()
            {
                GameData.LastProcessedItem = _lastProcessedItem;
                GameData.ReceivedRelics = _receivedRelics;
                GameData.ReceivedFigurines = _receivedFigurines;
                GameData.ReceivedCharacters = _receivedCharacters;
                GameData.CheckedYaku = _checkedYaku;
                GameData.MaxStages = _maxStages;
                GameData.CheckedAchievements = _checkedAchievements;
                GameData.HighestDifficulty = _highestDifficulty;
                GameData.MinDifficulty = _minDifficulty;
                GameData.UnprocessedItems = _unprocessedItems;

                ArchipelagoClient.ServerData.Index = _lastProcessedItem;
            }
            public SaveData() {
                this._lastProcessedItem = GameData.LastProcessedItem;
                this._receivedRelics = GameData.ReceivedRelics;
                this._receivedFigurines= GameData.ReceivedFigurines;
                this._receivedCharacters= GameData.ReceivedCharacters;
                this._checkedYaku = GameData.CheckedYaku;
                this._maxStages = GameData.MaxStages;
                this._checkedAchievements = GameData.CheckedAchievements;
                this._highestDifficulty = GameData.HighestDifficulty;
                this._minDifficulty = GameData.MinDifficulty;
                this._unprocessedItems = GameData.UnprocessedItems;
            }
        }

        public static async Task SaveAsync()
        {
            if (_savePath != null)
            {
                var path = _savePath +$"/{Seed}.json";
                _saveData = new SaveData();

                await using FileStream source = File.OpenRead(_savePath);
                await using FileStream destination = File.Create(_savePath + ".bak");
                await source.CopyToAsync(destination);
                await using FileStream createStream = File.Create(_savePath);
                try
                {
                    await JsonSerializer.SerializeAsync(createStream, _saveData);
                }
                catch (Exception)
                {
                    BepinLogger.LogError($"Could not save file {_saveData}");
                }

            }
        }

        public static async Task LoadSaveAsync()
        {
            var path = _savePath + $"/{Seed}.json";
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
        }
    }
}
