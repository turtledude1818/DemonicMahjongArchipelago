using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using MaJiang;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonicMahjongArchipelago
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ArchipelagoPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "DemonicMahjongArchipelago";
        public const string PLUGIN_NAME = "Demonic Mahjong Archipelago";
        public const string PLUGIN_VERSION = "0.0.1";

        public const string ModDisplayInfo = $"{PLUGIN_NAME} v{PLUGIN_VERSION}";
        private const string APDisplayInfo = $"Archipelago v{ArchipelagoClient.APVersion}";
        public static ManualLogSource BepinLogger;
        public static ArchipelagoClient ArchipelagoClient;

        // On Startup
        public override void Load()
        {
            // Plugin startup logic
            BepinLogger = Log;
            BepinLogger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            ArchipelagoClient = new ArchipelagoClient();
            GameData.Init(ArchipelagoClient);

            //Injections

            //ClassInjector.RegisterTypeInIl2Cpp<GameData.SaveData>();

            harmonyPatches();

        }

        public static void harmonyPatches()
        {
            var harmony = new Harmony("DMJ");
            harmony.PatchAll(typeof(ReversePatches));

            harmony.PatchAll(typeof(BlockingPatches));
            harmony.PatchAll(typeof(OverridePatches));
            harmony.PatchAll(typeof(CheckingPatches));
            harmony.PatchAll(typeof(GameSetupPatches));
            harmony.PatchAll(typeof(UIPatches));
            harmony.PatchAll(typeof(ReplaceFieldPatches));

            //harmony.PatchAll(typeof(DebugPatches));
        }
    }
}
