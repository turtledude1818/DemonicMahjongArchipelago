using I2.Loc;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using MaJiang;
using MaJiang.Achievement;
using MaJiang.Achievement.Runtime;
using MaJiang.DataConstruct;
using MaJiang.GameMap;
using MaJiang.UI;
using MaJiang.UI.Bag;
using MaJiang.UICtrl.BaseClass;
using MaJiang.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace DemonicMahjongArchipelago
{
    internal class ArchipelagoUI
    {
        public class ArchipelagoPanel : BaseUIPanelCtrl
        {
            ArchipelagoPanel(IntPtr ptr) : base(ptr) { }

            public void SetOpenParams(object[] openParams)
            {

            }

        }

        // Stealing presets from the game and modifying them to make Archipelago UI
        private static UnityEngine.UI.Button archipelagoSettingBtn;
        private static GameObject prefab;
        private static OptionMenuPanelCtrl optionMenu;
        private static readonly BepInEx.Logging.ManualLogSource BepinLogger = ArchipelagoPlugin.BepinLogger;
        private static TextMeshProUGUI[] inputs = new TextMeshProUGUI[3];
        private static AchievementRuntime achievementTemplate;
        public static void AddArchipelagoSettingButton(OptionMenuPanelCtrl __instance)
        {
            if (prefab == null)
            {
                prefab = MakePrefab();
            }
            if (archipelagoSettingBtn == null)
            {
                optionMenu = __instance;
                archipelagoSettingBtn = UnityEngine.Object.Instantiate(__instance.settingBtn);
                archipelagoSettingBtn.GetComponentInChildren<TextMeshProUGUI>().SetText("Archipelago");
                archipelagoSettingBtn.name = "ArchipelagoSetting";
                archipelagoSettingBtn.transform.SetParent(__instance.settingBtn.transform.parent, false);
                archipelagoSettingBtn.onClick.RemoveAllListeners();
                archipelagoSettingBtn.onClick.AddListener(new Action(ArchipelagoSettingsWindow));
            }
        }

        private static void ArchipelagoSettingsWindow()
        {
            if (prefab == null)
            {
                prefab = MakePrefab();
            }
            Il2CppSystem.Object[] openParams = { optionMenu };
            // The compiler will crash if the original is used for some reason, so use a Reverse Patch
            ReversePatches.OpenUI(PopUIManager.Instance, prefab, openParams);
            GameObject.Destroy(prefab);
            // Add the onClick listener to the connect button
            //var settingPanel = GameObject.Find("ArchipelagoConnectPanel");
            //if (settingPanel != null)
            //{
            //    var connectButton = settingPanel.transform.Find("ConnectButton");
            //    if (connectButton != null)
            //    {
            //        connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            //        connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));
            //    }
            //}
            var connectButton = GameObject.Find("ConnectButton");
            if (connectButton == null)
            {
                BepinLogger.LogWarning("Couldn't find Connect Button");
                return;
            }
            connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));
            var disconnectButton = GameObject.Find("DisconnectButton");
            if (disconnectButton == null)
            {
                BepinLogger.LogWarning("Couldn't find Disconnect Button");
                return;
            }
            disconnectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            disconnectButton.GetComponent<Button>().onClick.AddListener(new Action(onDisconnectClicked));

            inputs[0] = GameObject.Find("Server URI").GetComponent<TMP_InputField>()
                .textComponent.Cast<TextMeshProUGUI>();
            inputs[1] = GameObject.Find("Player Name").GetComponent<TMP_InputField>()
                .textComponent.Cast<TextMeshProUGUI>();
            inputs[2] = GameObject.Find("Password").GetComponent<TMP_InputField>()
                .textComponent.Cast<TextMeshProUGUI>();
        }
        public static void onConnectClicked()
        {
            ArchipelagoClient.ServerData.Uri = inputs[0].text[..^1];//.Replace("\u200b", ""); ;
            ArchipelagoClient.ServerData.SlotName = inputs[1].text[..^1];//.Replace("\u200b", "");
            ArchipelagoClient.ServerData.Password = inputs[2].text[..^1];//.Replace("\u200b", "");
            //BepinLogger.LogMessage("Connecting to Server");
            //BepinLogger.LogMessage($"URI: {inputs[0].text}\n SlotName: {inputs[1].text}\n Password: {inputs[2].text}");
            ArchipelagoPlugin.ArchipelagoClient.Connect();
            //BepinLogger.LogMessage($"Status: {(ArchipelagoClient.Authenticated ? "" : "Not")} Connected");
        }

        public static void onDisconnectClicked()
        {
            Task.Run(() => ArchipelagoPlugin.ArchipelagoClient.Disconnect());
        }

        private static GameObject MakePrefab()
        {
            var prefab = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MapMenuSettingPanel);
            prefab.SetActive(false);
            prefab.name = "ArchipelagoConnection";

            //var prevComponent = prefab.GetComponent<SettingsPanel>();
            //var component = prefab.AddComponent<ArchipelagoPanel>();
            //CopyComponent<IManagedUI>(component, prevComponent.Cast<IManagedUI>());
            //Component.Destroy(prevComponent);

            var bg = prefab.transform.Find("Bg");
            var title = bg.Find("LineTitle_1").Find("Text");
            title.GetComponent<TextMeshProUGUI>().SetText("Archipelago Connect");
            var content = bg.Find("Scroll View").GetChild(0).GetChild(0);
            var left = content.GetChild(0);
            var right = content.GetChild(1);
            for (int i = 12; i > 2; i--)
            {
                GameObject.Destroy(left.GetChild(i).gameObject);
                GameObject.Destroy(right.GetChild(i).gameObject);
            }

            var input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            input.GetComponent<TMP_InputField>().placeholder.Cast<TextMeshProUGUI>().text = "";
            input.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 100);
            input.transform.GetChild(0).Find("Text").GetComponent<TextMeshProUGUI>().enableAutoSizing = true;
            var uri = GameObject.Instantiate(input);
            uri.transform.SetParent(right);
            uri.name = "Server URI";
            left.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Server URI";
            var playerName = GameObject.Instantiate(input);
            playerName.transform.SetParent(right);
            playerName.name = "Player Name";
            left.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Player Name";
            var password = GameObject.Instantiate(input);
            password.transform.SetParent(right);
            password.name = "Password";
            left.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Password";
            password.GetComponent<TMP_InputField>().contentType = TMP_InputField.ContentType.Password;

            var connectButton = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MainMenuTestGivenPanel
                .transform.Find("PopUI").Find("ConfirmButton").gameObject, right);
            connectButton.name = "ConnectButton";
            Component.Destroy(connectButton.transform.GetChild(0).GetComponent<Localize>());
            connectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Connect";
            var disconnectButton = GameObject.Instantiate(connectButton, left);
            disconnectButton.name = "DisconnectButton";
            disconnectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Disconnect";

            GameObject.Destroy(input);
            Component.Destroy(title.GetComponent<Localize>());
            for (int i = 2; i >= 0; i--)
            {
                GameObject.Destroy(right.GetChild(i).gameObject);
                Component.Destroy(left.GetChild(i).GetComponent<Localize>());
            }

            return prefab;
        }

        public static void CreateInfoPanel(string title, string message, bool reconnect = false)
        {
            var popup = GameObject.Instantiate(GameMapMgr.Instance.uiConfig.commonPopUp);
            popup.name = "NotConnectedPopup";
            var panel = popup.transform.Find("Panel");
            var titleObject = panel.Find("Title").Find("Text");
            Component.Destroy(titleObject.GetComponent<Localize>());
            titleObject.GetComponent<TextMeshProUGUI>().text = title;
            var textObject = panel.Find("TextContent");
            Component.Destroy(textObject.GetComponent<Localize>());
            textObject.GetComponent<TextMeshProUGUI>().text = message;
            var button = panel.Find("FunctionButtons").Find("Right");
            button.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Confirm";
            if (reconnect)
            {
                var reconnectButton = panel.Find("FunctionButtons").Find("Left");
                button.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Reconnect";
            }

            ReversePatches.OpenUI(PopUIManager.Instance, popup);
            GameObject.Find("NotConnectedPopup(Clone)").transform.Find("Panel").Find("FunctionButtons")
                .Find("Right").gameObject.SetActive(true);
            if (reconnect)
            {
                var reconnectButton = GameObject.Find("NotConnectedPopup(Clone)")
                    .transform.Find("Panel").Find("FunctionButtons").Find("Left");
                reconnectButton.GetComponent<Button>().onClick.RemoveAllListeners();
                reconnectButton.GetComponent<Button>().onClick
                    .AddListener(new Action(ArchipelagoPlugin.ArchipelagoClient.Connect));
                reconnectButton.gameObject.SetActive(true);
            }
            GameObject.Destroy(popup);
        }

        private static void makeAchievementTemplate()
        {
            AchievementRuntime template;
            AchievementManager.Instance.TryGetAchievementByOnlyID("200069", out template);
            if (template == null)
            {
                throw new IndexOutOfRangeException("Could not create achievement pop template" +
                    " from onlyID 200069");
            }

            achievementTemplate = template;
        }

        public static void ItemPopPanel(IItemInfo item)
        {
            try
            {
                if (achievementTemplate == null) makeAchievementTemplate();
                achievementTemplate.UnlockItem = item;
                AchievementManager.Instance.PopEnqueue(achievementTemplate);
            }
            catch (IndexOutOfRangeException e)
            {
                BepinLogger.LogError(e);
            }
        }

        private static T CopyComponent<T>(T component, T other) where T : Component
        {

            PropertyInfo[] pinfos = typeof(T).GetProperties();
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(component, pinfo.GetValue(other));
                    }
                    catch { }
                    ;
                }
            }
            FieldInfo[] finfos = typeof(T).GetFields();
            foreach (var finfo in finfos)
            {
                finfo.SetValue(component, finfo.GetValue(other));
            }
            return component as T;
        }
    }
}
