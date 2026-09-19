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
        // set to true at the beginning to ensure proper state initialization
        private bool _newHSceneStarted = true;
        private static ManualLogSource _Log;
        private static RobotMovement _robotMovement;
        private readonly Stopwatch _sw;
        
        private KKS_SexRobotControllerPlugin()
        {
            // create a stopwatch instance, but don't start it
            _sw = Stopwatch.StartNew();
            _sw.Reset();
        }
        
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
            _sw.Stop();
            _hFlags = null;
            RobotMovement.GetInstance().HSceneEnding();
        }

        private void OnHSceneUpdate(HSprite _hSprite)
        {
            try
            {
                if (_hSprite == null)
                    return;

                // if previously a H-Scene was played and ended 
                // and a new one is now being started, clear previous values
                if (_newHSceneStarted)
                {
                    _sw.Restart();
                    _newHSceneStarted = false;
                    RobotMovement.GetInstance().HSceneEnding();
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

                        // check in what way the animation should be tracked
                        // (if insertion/penetration, calculate L0 based on the Penis bones. If e.g. handjob, footjob, etc., then calculate the L0 based on the female target)
                        BoneAnimationDefiner.animationFemaleTargetDictionary.TryGetValue(_robotMovement.AnimationName, out BoneAnimationDefiner.FemaleTargetType currentFemaleTargetType);
                        _robotMovement.AnimationIsInsertion = currentFemaleTargetType switch
                        {
                            BoneAnimationDefiner.FemaleTargetType.VAGINAL
                            or BoneAnimationDefiner.FemaleTargetType.VAGINALSWAP or BoneAnimationDefiner.FemaleTargetType.ANAL
                            => true,
                            _ => false,
                        };

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

        //HandlePause: called before/after sex (e.g. pos. select, initialize)
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
                    _sw.Reset();
                    _newHSceneStarted = true;
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
            catch (NullReferenceException ex)
            {
                // if a NullReferenceException is thrown, it could be caused the objects weren't properly initialized
                // or because the player returned to the title without cleanly exiting the current H-Scenee playing
                // therefore, attempt a reload by clearing the currently set values (requires animation change/reload)
                Logger.LogDebug("Error in Update() (NullReferenceException): " + ex.ToString());
                Logger.LogDebug("Clearing set values by calling 'ForceHSceneReload()'. Reload or change animations for changes to take effect.");
                ForceHSceneReload();
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Error in Update(): " + ex.ToString());
            }
        }

        internal void ForceHSceneReload()
        {
            _sw.Reset();
            _hFlags = null;
            _newHSceneStarted = true;
        }
    }
}
