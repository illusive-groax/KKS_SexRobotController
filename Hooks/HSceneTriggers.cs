using HarmonyLib;
using KKS_SexRobotController.Helpers;
using KKS_SexRobotController.Plugin;
using KKS_SexRobotController.RobotController;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KKS_SexRobotController.Hooks
{
    internal static partial class Hooks
    {

        private static class HSceneTriggers
        {
            private static bool _guiButtonCreated = false;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.InitHeroine))]
            internal static void InitHeroine(HSprite __instance)
            {
                if (!_guiButtonCreated)
                    CreateConfigButton();
                GetController().OnInitHeroine(ref __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.OnChangePlaySelect))]
            internal static void OnChangePlaySelect(HSprite __instance)
            {
                GetController().HandlePause(ref __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.SpeedUpClick))]
            internal static void SpeedUpClick(HFlag __instance)
            {
                GetController().OnSpeedChange(__instance);
            }

            private static void CreateConfigButton()
            {
                try
                {
                    GameObject original = GameObject.Find(StringConstants.ButtonPath_Settings);
                    if (original == null)
                        return;
                    // Create connect robot button by instantiating main button, changing it's name, text label, and adding a new listener to handle click events
                    KKS_SexRobotControllerPlugin.BtnConnectRobot = UnityEngine.Object.Instantiate(original, original.transform.parent).transform;
                    KKS_SexRobotControllerPlugin.BtnConnectRobot.name = StringConstants.ButtonConnectRobot_Name;
                    KKS_SexRobotControllerPlugin.BtnConnectRobotText = KKS_SexRobotControllerPlugin.BtnConnectRobot.GetComponentInChildren<TextMeshProUGUI>();
                    KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonConnectRobot_Text;
                    Button newButton = KKS_SexRobotControllerPlugin.BtnConnectRobot.GetComponentInChildren<Button>();
                    newButton.onClick.RemoveAllListeners();
                    newButton.onClick.AddListener(delegate
                    {
                        if (KKS_SexRobotControllerPlugin.SerialPortConnected.Value)
                        {
                            KKS_SexRobotControllerPlugin.BtnDisconnectRobotClicked = true;
                        }
                        else
                        {
                            KKS_SexRobotControllerPlugin.BtnConnectRobotClicked = true;
                        }
                    });
                    _guiButtonCreated = true;
                }
                catch (Exception e)
                {
                    KKS_SexRobotControllerPlugin.LogDebug("Error upon creating Settings Button: " + e.ToString());
                }
            }
        }
    }
}
