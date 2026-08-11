using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKS_SexRobotController.Helpers;
using KKS_SexRobotController.RobotController;
using System;
using System.Diagnostics;

namespace KKS_SexRobotController.Plugin
{
    [BepInProcess(StringConstants.GAME_NAME)]
    [BepInProcess(StringConstants.GAME_VR_NAME)]
    [BepInPlugin(StringConstants.PLUGIN_GUID, StringConstants.PLUGIN_NAME, StringConstants.PLUGIN_VERSION)]

    internal partial class KKS_SexRobotControllerPlugin : BaseUnityPlugin
    {
        private HFlag _hFlags;
        private bool _hSceneEnded = false;
        private static ManualLogSource _Log;
        private static RobotMovement _robotMovement;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        private void Start()
        {
            _serialPortConnection = SerialPortConnection.GetInstance();
            _robotMovement = RobotMovement.GetInstance();
            Hooks.Hooks.InstallHooks();
            Harmony.CreateAndPatchAll(typeof(KKS_SexRobotControllerPlugin));
        }

        private void Awake()
        {
            _Log = base.Logger;
            SetupPluginConfigurations();
        }

        private void OnDestroy()
        {
            _sw.Reset();
            _hFlags = null;
            _hSceneEnded = true;
            RobotMovement.GetInstance().HSceneEnding();
        }

        //called only on scene load/initialization
        internal void OnHSceneLoad(HSceneProc __instance)
        {
            try
            {
                // if previously an H-Scene was played and ended 
                // and a new one is now being started, clear previous values
                if (_hSceneEnded)
                {
                    _robotMovement.Player = null;
                    _robotMovement.Females = null;
                    _hSceneEnded = false;
                }
                if (_robotMovement.Player == null)
                    _robotMovement.Player = __instance.male;
                if (_robotMovement.Females == null)
                    _robotMovement.Females = __instance.lstFemale.FindAll(female => female != null).ToArray();
                _robotMovement.UpdatePosition = false;
                _robotMovement.SpeedChanged = false;
            }
            catch (Exception e)
            {
                Logger.LogDebug("Error in OnHSceneLoad(): " + e.ToString());
            }
        }
        private void OnHSceneUpdate(HSprite _hSprite)
        {
            try
            {
                if (_hSprite == null)
                    return;
                // if previously an H-Scene was played and ended 
                // and a new one is now being started, clear previous values
                if (_hSceneEnded)
                {
                    _robotMovement.Player = null;
                    _robotMovement.Females = null;
                    _hSceneEnded = false;
                }
                if (_robotMovement.Females == null && _hSprite.females != null)
                {
                    _robotMovement.Females = _hSprite.females.FindAll(female => female != null).ToArray();
                }
                OnHSceneUpdate(_hSprite.flags);
            }
            catch (Exception e)
            {
                Logger.LogDebug("Error in OnHSceneUpdate(): " + e.ToString());
            }
        }

        private void OnHSceneUpdate(HFlag _flag)
        {
            try
            {
                if (_flag != null)
                {
                    _hFlags = _flag;
                    // check if the animation or the animation speed has changed
                    //if so, update the animation values
                    if (_robotMovement.AnimationName != _hFlags.nowAnimationInfo.nameAnimation)
                    {
                        _robotMovement.AnimationChanged = true;
                        string currAnimName = _hFlags.nowAnimationInfo.nameAnimation;
                        if (currAnimName != null
                            && currAnimName != ""
                            && currAnimName != StringConstants.KKS_STARTING_ANIMATION_NAME_TO_IGNORE)
                            CheckAnimationName(_hFlags.nowAnimationInfo.nameAnimation);
                        _robotMovement.AnimationName = currAnimName;
                    }

                    // in VR, the _robotMovement.Player doesn't get set
                    // therefore, check here if _robotMovement.Player is set
                    if (_robotMovement.Player == null)
                        _robotMovement.Player = _hFlags.player.chaCtrl;

                    //check if positions should be read from file
                    if (ReadAnimationsFromFile.Value && !FileIsRead)
                    {
                        try
                        {
                            // read positions from file
                            FileHandler.ReadAnimationsFromFile();
                        }
                        catch (Exception e)
                        {
                            Logger.LogDebug("Error updating Animation dictionary: " + e.ToString());

                        }
                        FileIsRead = true;
                    }
                    else if (!ReadAnimationsFromFile.Value)
                    {
                        // if disabled, set read to false, to enable live updates
                        FileIsRead = false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug("Error in OnHSceneUpdate(): " + e.ToString());
            }
        }

        internal static void CheckAnimationName(string currAnimName)
        {
            _robotMovement = RobotMovement.GetInstance();
            // check current animation name (for finding unregistered sex-animations)
            // verify that animation doesn't exist and isn't already printed
            if (WriteAnimationsToFile.Value &&
                _robotMovement.AnimationName != currAnimName &&
                !BoneAnimationDefiner.animationFemaleTargetDictionary.ContainsKey(_robotMovement.AnimationName))
            {
                // set previous to the current to avoid multiple rewrites on current animation refresh
                FileHandler.WriteToFile(_robotMovement.AnimationName);
                LogInfo("The animation name '" + _robotMovement.AnimationName + "' was written to file!");
            }
        }

        //OnInitHeroine: always called
        internal void OnInitHeroine(ref HSprite hSprite)
        {
            if (hSprite != null)
                OnHSceneUpdate(hSprite);
        }

        //called on speed change
        internal void OnSpeedChange(HFlag hFlag)
        {
            OnHSceneUpdate(hFlag);
        }

        //HandlePause: called before/after sex  (e.g. pos. select, initialize)
        internal void HandlePause(ref HSprite hSprite)
        {
            if (hSprite != null)
                OnHSceneUpdate(hSprite);
        }

        internal static void LogInfo(string log)
        {
            _Log.LogInfo(log);
        }

        internal static void LogDebug(string log)
        {
            _Log.LogDebug(log);
        }

        private void Update()
        {
            try
            {
                _serialPortConnection.CheckButtonAndSerialConnState();

                // Return if not in an HScene
                if (_hFlags == null)
                {
                    return;
                }

                if (_hFlags.isHSceneEnd)
                {
                    // H-Scene is ending, set flag and return
                    _hSceneEnded = true;
                    return;
                }

                // Get ms elapsed since current stopwatch interval
                float msElapsed = _sw.ElapsedMilliseconds;

                // If the ms elapsed is greater than the period based on the robot's update frequency then
                // stop the stopwatch, call the robot update function, and restart the stopwatch
                if (msElapsed >= (1000.0 / SexRobotUpdateFrequencyConfig.Value))
                {
                    _sw.Stop();

                    // check here if the speed needs to be updated, as updates only handle loops and not speed adjustment
                    if (_robotMovement.NowAnimStateName != _hFlags.nowAnimStateName && !_robotMovement.AnimationChanged)
                    {
                        _robotMovement.SpeedChanged = true;
                        _robotMovement.NowAnimStateName = _hFlags.nowAnimStateName;
                    }

                    _robotMovement.UpdateAnimationStatus();
                    _sw.Restart();
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Error in Update(): " + ex.ToString());
            }
        }
    }
}
