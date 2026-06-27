using I2.Loc;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using MaJiang;
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
            BepinLogger.LogInfo("Clicked Archipelago button");
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
                BepinLogger.LogInfo("Couldn't find Connect Button");
                return;
            }
            connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));
            BepinLogger.LogInfo("Opened UI");
        }
        public static void onConnectClicked()
        {
            BepinLogger.LogInfo("Connect Clicked");
            //ArchipelagoClient.ServerData.Uri = inputs[0].text;
            //ArchipelagoClient.ServerData.SlotName = inputs[1].text;
            //ArchipelagoClient.ServerData.Password = inputs[2].text;
            BepinLogger.LogInfo("Connecting to Server");
            ArchipelagoPlugin.ArchipelagoClient.Connect();
            BepinLogger.LogInfo($"Status: {(ArchipelagoClient.Authenticated ? "Not" : "")} Connected");
        }

        private static GameObject MakePrefab2()
        {
            var prefab = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MainMenuTestGivenPanel);
            prefab.name = "ArchipelagoConnectPanel";

            //var connectButton = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MainMenuTestGivenPanel
            //    .transform.Find("PopUI").Find("ConfirmButton").gameObject, prefab.transform.GetChild(0));
            var connectButton = prefab.transform.GetChild(0).Find("ConfirmButton");
            connectButton.name = "ConnectButton";
            //connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            //connectButton.GetComponent<Button>().onClick.AddListener(new Action (onConnectClicked));
            //connectButton.name = "ConnectButton";
            //Component.Destroy(connectButton.transform.GetChild(0).GetComponent<Localize>());
            //connectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Connect";
            //connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            //connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));
            //var panel = prefab.transform.GetChild(1);
            //var title = panel.Find("Title").Find("Text");
            //var text = panel.Find("TextContent");
            //var connectButton = panel.Find("FunctionButtons").GetChild(1);
            //var closeButton = panel.Find("FunctionButtons").GetChild(2);
            //
            //connectButton.gameObject.SetActive(true);
            //closeButton.gameObject.SetActive(true);
            //
            //GameObject.Destroy(panel.Find("FunctionButtons").GetChild(1));
            //Component.Destroy(title.GetComponent<Localize>());
            //Component.Destroy(text.GetComponent<Localize>());
            //
            //connectButton.name = "ConnectButton";
            //closeButton.name = "CloseButton";
            //connectButton.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Connect";
            //closeButton.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Close";
            //
            //connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            //connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));
            //bg.Find("Close").SetSiblingIndex(2);
            //for (int i = bg.childCount; i > 2; i--)
            //{
            //    GameObject.Destroy (bg.GetChild(i).gameObject);
            //}
            //var connectButton = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MainMenuTestGivenPanel
            //    .transform.Find("PopUI").Find("ConfirmButton").gameObject, bg);
            //connectButton.name = "ConnectButton";
            //Component.Destroy(connectButton.transform.GetChild(0).GetComponent<Localize>());
            //connectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Connect";
            //connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            //connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));

            prefab.SetActive(false);
            return prefab;
        }

        private static GameObject MakePrefab()
        {
            var prefab = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MapMenuSettingPanel);
            prefab.SetActive(false);
            prefab.name = "ArchipelagoSetting";

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
            inputs[0] = uri.GetComponent<TMP_InputField>().textComponent.Cast<TextMeshProUGUI>();
            left.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Server URI";
            var playerName = GameObject.Instantiate(input);
            playerName.transform.SetParent(right);
            playerName.name = "Player Name";
            inputs[1] = playerName.GetComponent<TMP_InputField>().textComponent.Cast<TextMeshProUGUI>();
            left.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Player Name";
            var password = GameObject.Instantiate(input);
            password.transform.SetParent(right);
            password.name = "Password";
            inputs[2] = password.GetComponent<TMP_InputField>().textComponent.Cast<TextMeshProUGUI>();
            left.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Password";
            password.GetComponent<TMP_InputField>().contentType = TMP_InputField.ContentType.Password;

            var connectButton = GameObject.Instantiate(MaJiang.GameMap.GameMapMgr.Instance.uiConfig.MainMenuTestGivenPanel
                .transform.Find("PopUI").Find("ConfirmButton").gameObject, right);
            connectButton.name = "ConnectButton";
            Component.Destroy(connectButton.transform.GetChild(0).GetComponent<Localize>());
            connectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Connect";
            connectButton.GetComponent<Button>().onClick.RemoveAllListeners();
            connectButton.GetComponent<Button>().onClick.AddListener(new Action(onConnectClicked));
            //var disconnectButton = GameObject.Instantiate(button, left);
            //disconnectButton.name = "DisconnectButton";
            //disconnectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Disconnect";

            GameObject.Destroy(input);
            Component.Destroy(title.GetComponent<Localize>());
            for (int i = 2; i >= 0; i--)
            {
                GameObject.Destroy(right.GetChild(i).gameObject);
                Component.Destroy(left.GetChild(i).GetComponent<Localize>());
            }

            return prefab;
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
