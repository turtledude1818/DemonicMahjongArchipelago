using MaJiang;
using MaJiang.Achievement.Runtime;
using MaJiang.DataConstruct.Character;
using MaJiang.DataConstruct.MaJiang;
using MaJiang.DataConstruct.Relic;
using MaJiang.DataConstruct.XiaoChouPai;
using MaJiang.GM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonicMahjongArchipelago
{
    internal class GameData
    {
        private static ArchipelagoClient Client;

        // Game Data Management
        public static GlobalStaticDataManager GlobalStaticDataManagerInstance;
        public static GlobalDataCenter GlobalDataCenterInstance;
        public static GameManager GameManagerInstance;
        public static Saver SaverInstance;
        public static bool InGame = false;
        public static bool InBattle = false;
        public static int Difficulty;
        public static CharacterID Character;

        // Items
        //public static HashSet<RelicId> startingRelics = new HashSet<RelicId> { RelicId.ChuXuGuan };
        //public static HashSet<XiaoChou> startingFigurines = new HashSet<XiaoChou> { XiaoChou.Bao4ShiXiaoYao, XiaoChou.JuCaiHe, XiaoChou.HuPengGui };
        //public static HashSet<CharacterID> startingCharacters = new HashSet<CharacterID> { CharacterID.TanCaiJiangShi };
        // Following arrays don't include starting items
        public static HashSet<RelicId> receivedRelics = new HashSet<RelicId> { RelicId.ChuXuGuan };
        public static HashSet<XiaoChou> receivedFigurines = new HashSet<XiaoChou> { XiaoChou.Bao4ShiXiaoYao, XiaoChou.JuCaiHe, XiaoChou.HuPengGui };
        public static HashSet<CharacterID> receivedCharacters = new HashSet<CharacterID> { CharacterID.TanCaiJiangShi };

        // Locations
        public static HashSet<FanZhong> checkedYaku = [];
        public static Dictionary<CharacterID, int> maxStages = new Dictionary<CharacterID, int>();
        public static HashSet<AchievementRuntime> checkedAchievements = new HashSet<AchievementRuntime>();
        public static int highestDifficulty = 0;


        private static BepInEx.Logging.ManualLogSource Log = ArchipelagoPlugin.BepinLogger;

        public static bool checkLocation(object location, string type, int misc)
        {
            int id = 0;
            switch (type)
            {
                case "Yaku":

                default:
                    break;
            }
            if (id == 0) return false;
            Client.checkLocation(id);
            return true;
        }

        public static void setUpGameData()
        {
            GlobalDataCenterInstance = GlobalDataCenter.Instance;
            GlobalStaticDataManagerInstance = GlobalDataCenterInstance.staticDataMgr;
            GameManagerInstance = MaJiang.GM.GameManager.Instance;
            SaverInstance = GameManagerInstance.Saver;

            //Log.LogInfo("Game setup complete");
            //Log.LogInfo($"DataCenter null: {GlobalDataCenterInstance == null}" +
            //    $"StaticDataManager null: {GlobalStaticDataManagerInstance == null}" +
            //    $"GameManager null: {GameManagerInstance == null}" +
            //    $"Saver null: {SaverInstance == null}");
        }
        public static void enterGame()
        {
            InGame = true;
            Difficulty = MaJiang.Difficulty.DifficultyManager.Instance.CurDifficulty.index;
            Character = SaverInstance.LastUsedCharacterID;
            Log.LogInfo($"Entering game as {ItemNames.CharacterNames[Character]} on difficulty {Difficulty}");
        }

        public static void unlockRelic(RelicId relic)
        {
            RelicId[] relicIds = { relic };
            //ReversePatches.unlockRelic(ArchipelagoData.SaverInstance, relicIds);
        }

        public static CharacterID[] UnlockedChars()
        {
            return receivedCharacters.ToArray();
        }
        public static RelicId[] UnlockedRelics()
        {
            //return startingRelics.Concat(receivedRelics).ToArray();
            return receivedRelics.ToArray();
        }
        public static XiaoChou[] UnlockedFigurines()
        {
            return receivedFigurines.ToArray();
        }

        // Unimplemented
        public static void ClearAllData()
        {

        }

        public static bool SetClient(ArchipelagoClient client)
        {
            if (Client == null)
            {
                Client = client;
                return true;
            }
            return false;
        }
    }
}
