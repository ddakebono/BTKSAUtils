using System.Text;
using ABI_RC.Core.Player;
using BTKSAUtils.Components.AvatarParamInterpolator.Interpolation;
using BTKSAUtils.Config;
using BTKUILib;
using UnityEngine;

namespace BTKSAUtils.Components.AvatarParamInterpolator;

public class AvatarParamInterpolator
{
    public static NetworkTickSystem NetworkTickSystem;
    public static readonly BTKBoolConfig InterpolatorToggle = new("Param Interpolation", "Enable Param Interpolation", "Turns the interpolator on and off", true, null, false);
    public static readonly BTKBoolConfig CommonOSCOnly = new("Param Interpolation", "Only Interpolate Common Params", "Only interpolates common OSC params from VRCFT v1/v2 and newer Brainflow params, might not be needed but will reduce the amount of interpolated params", false, null, false);
    public static readonly BTKFloatConfig InterpolatorTime = new("Param Interpolation", "Interpolation Time", "This controls how long it takes for the interpolated value to reach the target", 0.05f, 0f, 1f, null, false);
    public static readonly BTKFloatConfig MaxInterpolationDistance = new("Param Interpolation", "Max Interpolation Distance", "Set how far away a player must be before interpolation stops being used", 8f, 0f, 20f, null, false);

    private static readonly List<PlayerContainer> PlayerContainers = new();
    private static PlayerContainer _selectedPlayerContainer;

    private readonly List<string> _disabledPlayerIDs = new();

    private static string[] _defaultParams =
    {
        "MovementX", "MovementY", "Grounded", "Emote", "GestureLeft", "GestureRight", "Toggle", "Sitting", "Crouching", "CancelEmote", "Prone", "Flying"
    };

    private static string[] _commonFloatParams =
    {
        "BFI/NeuroFB/FocusLeft",
        "BFI/NeuroFB/FocusRight",
        "BFI/NeuroFB/FocusAvg",
        "BFI/NeuroFB/RelaxLeft",
        "BFI/NeuroFB/RelaxRight",
        "BFI/NeuroFB/RelaxAvg",
        "BFI/Biometrics/HeartBeatsPerMinute",
        "BFI/Biometrics/HeartBeatsPerSecond",
        "BFI/Biometrics/OxygenPercent",
        "BFI/Biometrics/BreathsPerMinute",
        "BFI/PwrBands/Left/Alpha",
        "BFI/PwrBands/Left/Beta",
        "BFI/PwrBands/Left/Theta",
        "BFI/PwrBands/Left/Delta",
        "BFI/PwrBands/Left/Gamma",
        "BFI/PwrBands/Right/Alpha",
        "BFI/PwrBands/Right/Beta",
        "BFI/PwrBands/Right/Theta",
        "BFI/PwrBands/Right/Delta",
        "BFI/PwrBands/Right/Gamma",
        "BFI/PwrBands/Avg/Alpha",
        "BFI/PwrBands/Avg/Beta",
        "BFI/PwrBands/Avg/Theta",
        "BFI/PwrBands/Avg/Delta",
        "BFI/PwrBands/Avg/Gamma",
        "BFI/Info/BatteryLevel",
        "BFI/Info/DeviceConnected",
        "BFI/Info/SecondsSinceLastUpdate",
        "BFI/Addons/HueShift",
        "v2/EyeLeftX",
        "v2/EyeLeftY",
        "v2/EyeRightX",
        "v2/EyeRightY",
        "v2/EyeLidRight",
        "v2/EyeLidLeft",
        "v2/EyeLid",
        "v2/EyeSquintRight",
        "v2/EyeSquintLeft",
        "v2/EyeSquint",
        "v2/PupilDilation",
        "v2/PupilDiameterRight",
        "v2/PupilDiameterLeft",
        "v2/PupilDiameter",
        "v2/BrowPinchRight",
        "v2/BrowPinchLeft",
        "v2/BrowLowererRight",
        "v2/BrowLowererLeft",
        "v2/BrowInnerUpRight",
        "v2/BrowInnerUpLeft",
        "v2/BrowOuterUpRight",
        "v2/BrowOuterUpLeft",
        "v2/NoseSneerRight",
        "v2/NoseSneerLeft",
        "v2/NasalDilationRight",
        "v2/NasalDilationLeft",
        "v2/NasalConstrictRight",
        "v2/NasalConstrictLeft",
        "v2/CheekSquintRight",
        "v2/CheekSquintLeft",
        "v2/CheekPuffSuckRight",
        "v2/CheekPuffSuckLeft",
        "v2/JawOpen",
        "v2/MouthClosed",
        "v2/JawX",
        "v2/JawZ",
        "v2/JawClench",
        "v2/JawMandibleRaise",
        "v2/LipSuckUpperRight",
        "v2/LipSuckUpperLeft",
        "v2/LipSuckLowerRight",
        "v2/LipSuckLowerLeft",
        "v2/LipSuckCornerRight",
        "v2/LipSuckCornerLeft",
        "v2/LipFunnelUpperRight",
        "v2/LipFunnelUpperLeft",
        "v2/LipFunnelLowerRight",
        "v2/LipFunnelLowerLeft",
        "v2/LipPuckerUpperRight",
        "v2/LipPuckerUpperLeft",
        "v2/LipPuckerLowerRight",
        "v2/LipPuckerLowerLeft",
        "v2/MouthUpperUpRight",
        "v2/MouthUpperUpLeft",
        "v2/MouthLowerDownRight",
        "v2/MouthLowerDownLeft",
        "v2/MouthUpperDeepenRight",
        "v2/MouthUpperDeepenLeft",
        "v2/MouthUpperX",
        "v2/MouthLowerX",
        "v2/MouthCornerPullRight",
        "v2/MouthCornerPullLeft",
        "v2/MouthCornerSlantRight",
        "v2/MouthCornerSlantLeft",
        "v2/MouthDimpleRight",
        "v2/MouthDimpleLeft",
        "v2/MouthFrownRight",
        "v2/MouthFrownLeft",
        "v2/MouthStretchRight",
        "v2/MouthStretchLeft",
        "v2/MouthRaiserUpper",
        "v2/MouthRaiserLower",
        "v2/MouthPressRight",
        "v2/MouthPressLeft",
        "v2/MouthTightenerRight",
        "v2/MouthTightenerLeft",
        "v2/TongueOut",
        "v2/TongueX",
        "v2/TongueY",
        "v2/TongueRoll",
        "v2/TongueArchY",
        "v2/TongueShape",
        "v2/TongueTwistRight",
        "v2/TongueTwistLeft",
        "v2/SoftPalateClose",
        "v2/ThroatSwallow",
        "v2/NeckFlexRight",
        "v2/NeckFlexLeft",
        "v2/MouthX",
        "v2/MouthUpperUp",
        "v2/MouthLowerDown",
        "v2/MouthOpen",
        "v2/MouthSmileRight",
        "v2/MouthSmileLeft",
        "v2/MouthSadRight",
        "v2/MouthSadLeft",
        "v2/SmileFrownRight",
        "v2/SmileFrownLeft",
        "v2/SmileFrown",
        "v2/SmileSadRight",
        "v2/SmileSadLeft",
        "v2/SmileSad",
        "v2/LipSuckUpper",
        "v2/LipSuckLower",
        "v2/LipSuck",
        "v2/LipFunnelUpper",
        "v2/LipFunnelLower",
        "v2/LipFunnel",
        "v2/LipPuckerUpper",
        "v2/LipPuckerLower",
        "v2/LipPucker",
        "v2/NoseSneer",
        "v2/CheekSquint",
        "v2/CheekPuffSuck",
        "EyesX",
        "EyesY",
        "LeftEyeLid",
        "RightEyeLid",
        "CombinedEyeLid",
        "EyesWiden",
        "EyesDilation",
        "EyesPupilDiameter",
        "EyesSqueeze",
        "LeftEyeX",
        "LeftEyeY",
        "RightEyeX",
        "RightEyeY",
        "LeftEyeWiden",
        "RightEyeWiden",
        "LeftEyeSqueeze",
        "RightEyeSqueeze",
        "LeftEyeLidExpanded",
        "RightEyeLidExpanded",
        "CombinedEyeLidExpanded",
        "LeftEyeLidExpandedSqueeze",
        "RightEyeLidExpandedSqueeze",
        "CombinedEyeLidExpandedSqueeze",
        "JawRight",
        "JawLeft",
        "JawForward",
        "JawOpen",
        "MouthApeShape",
        "MouthUpperRight",
        "MouthUpperLeft",
        "MouthLowerRight",
        "MouthLowerLeft",
        "MouthUpperOverturn",
        "MouthLowerOverturn",
        "MouthPout",
        "MouthSmileRight",
        "MouthSmileLeft",
        "MouthSadRight",
        "MouthSadLeft",
        "CheekPuffRight",
        "CheekPuffLeft",
        "CheekSuck",
        "MouthUpperUpRight",
        "MouthUpperUpLeft",
        "MouthLowerDownRight",
        "MouthLowerDownLeft",
        "MouthUpperInside",
        "MouthLowerInside",
        "MouthLowerOverlay",
        "TongueLongStep1",
        "TongueLongStep2",
        "TongueDown",
        "TongueUp",
        "TongueRight",
        "TongueLeft",
        "TongueRoll",
        "TongueUpLeftMorph",
        "TongueUpRightMorph",
        "TongueDownLeftMorph",
        "TongueDownRightMorph",
        "JawX",
        "MouthUpper",
        "MouthLower",
        "MouthX",
        "MouthUpperInsideOverturn",
        "MouthLowerInsideOverturn",
        "SmileSadRight",
        "SmileSadLeft",
        "SmileSad",
        "TongueY",
        "TongueX",
        "TongueSteps",
        "PuffSuckRight",
        "PuffSuckLeft",
        "PuffSuck",
        "JawOpenApe",
        "JawOpenPuff",
        "JawOpenPuffRight",
        "JawOpenPuffLeft",
        "JawOpenSuck",
        "JawOpenForward",
        "JawOpenOverlay",
        "MouthUpperUpRightUpperInside",
        "MouthUpperUpRightPuffRight",
        "MouthUpperUpRightApe",
        "MouthUpperUpRightPout",
        "MouthUpperUpRightOverlay",
        "MouthUpperUpRightSuck",
        "MouthUpperUpLeftUpperInside",
        "MouthUpperUpLeftPuffLeft",
        "MouthUpperUpLeftApe",
        "MouthUpperUpLeftPout",
        "MouthUpperUpLeftOverlay",
        "MouthUpperUpLeftSuck",
        "MouthUpperUpUpperInside",
        "MouthUpperUpInside",
        "MouthUpperUpPuff",
        "MouthUpperUpPuffLeft",
        "MouthUpperUpPuffRight",
        "MouthUpperUpApe",
        "MouthUpperUpPout",
        "MouthUpperUpOverlay",
        "MouthUpperUpSuck",
        "MouthLowerDownRightLowerInside",
        "MouthLowerDownRightPuffRight",
        "MouthLowerDownRightApe",
        "MouthLowerDownRightPout",
        "MouthLowerDownRightOverlay",
        "MouthLowerDownRightSuck",
        "MouthLowerDownLeftLowerInside",
        "MouthLowerDownLeftPuffLeft",
        "MouthLowerDownLeftApe",
        "MouthLowerDownLeftPout",
        "MouthLowerDownLeftOverlay",
        "MouthLowerDownLeftSuck",
        "MouthLowerDownLowerInside",
        "MouthLowerDownInside",
        "MouthLowerDownPuff",
        "MouthLowerDownPuffLeft",
        "MouthLowerDownPuffRight",
        "MouthLowerDownApe",
        "MouthLowerDownPout",
        "MouthLowerDownOverlay",
        "MouthLowerDownSuck",
        "SmileRightUpperOverturn",
        "SmileRightLowerOverturn",
        "SmileRightOverturn",
        "SmileRightApe",
        "SmileRightOverlay",
        "SmileRightPout",
        "SmileLeftUpperOverturn",
        "SmileLeftLowerOverturn",
        "SmileLeftOverturn",
        "SmileLeftApe",
        "SmileLeftOverlay",
        "SmileLeftPout",
        "SmileUpperOverturn",
        "SmileLowerOverturn",
        "SmileApe",
        "SmileOverlay",
        "SmilePout",
        "PuffRightUpperOverturn",
        "PuffRightLowerOverturn",
        "PuffRightOverturn",
        "PuffLeftUpperOverturn",
        "PuffLeftLowerOverturn",
        "PuffLeftOverturn",
        "PuffUpperOverturn",
        "PuffLowerOverturn",
        "PuffOverturn",
        "Scale"
    };

    public void LateInit()
    {
        BTKSAUtils.Logger.Msg("Starting up Avatar Parameter Interpolator!");

        var timeStart = (double)Environment.TickCount / 1000;

        NetworkTickSystem = new NetworkTickSystem(20, timeStart, timeStart);

        CVRPlayerManager.Instance.OnPlayerEntityCreated += OnPlayerEntityCreated;
        CVRPlayerManager.Instance.OnPlayerEntityRecycled += OnPlayerEntityRecycled;

        QuickMenuAPI.OnWorldLeave += OnWorldLeave;
        InterpolatorToggle.OnConfigUpdated += OnToggleInterpolator;
        InterpolatorTime.OnConfigUpdated += OnConfigUpdated;
        CommonOSCOnly.OnConfigUpdated += OnToggleCommonOSCOnly;

        //Setup player select UI for individual user toggles
        var playerSelectCat = QuickMenuAPI.PlayerSelectPage.AddCategory("BTK Param Interpolation");
        var disableParam = playerSelectCat.AddToggle("Disable Param Interpolation", "Disable param interpolation on this specific user", false);
        disableParam.OnValueUpdated += OnPlayerSelectDisable;

        QuickMenuAPI.OnPlayerSelected += (s, s1) =>
        {
            disableParam.ToggleValue = _disabledPlayerIDs.Contains(s1);
        };

        if (!File.Exists("UserData\\BTKDisabledParamInterpolation.txt")) return;

        _disabledPlayerIDs.Clear();

        string[] lines = File.ReadAllLines("UserData\\BTKDisabledParamInterpolation.txt");

        foreach (string line in lines)
        {
            if (!String.IsNullOrWhiteSpace(line))
                _disabledPlayerIDs.Add(line);
        }
    }

    private void OnToggleCommonOSCOnly(bool obj)
    {
        foreach (var pair in PlayerContainers)
        {
            pair.ReapplyParamSetup();
        }
    }

    public static void OnApplyAdvAvatarSettingsPrefix(Animator animator)
    {
        PlayerContainer container = null;

        //Hijacked from the ApplyAdvancedAvatarSettings function
        for (int i = 0; i < PlayerContainers.Count; i++)
        {
            if (PlayerContainers[i].Animator != animator) continue;

            container = PlayerContainers[i];
            break;
        }

        _selectedPlayerContainer = container;
    }

    private void OnPlayerSelectDisable(bool state)
    {
        if (state)
        {
            var playerContainer = PlayerContainers.FirstOrDefault(x => x.Player.Uuid == QuickMenuAPI.SelectedPlayerID);

            if (playerContainer == null) return;

            playerContainer.Destroy();
            PlayerContainers.Remove(playerContainer);
            _disabledPlayerIDs.Add(playerContainer.Player.Uuid);
        }
        else
        {
            //Create new container
            _disabledPlayerIDs.Remove(QuickMenuAPI.SelectedPlayerID);

            var player = CVRPlayerManager.Instance.NetworkPlayers.FirstOrDefault(x => x.Uuid == QuickMenuAPI.SelectedPlayerID);
            OnPlayerEntityCreated(player);
        }

        SaveDisabledParamInterpolation();
    }

    private void OnConfigUpdated(float obj)
    {
        foreach(var player in PlayerContainers)
            player.UpdateInterpolatorTime(InterpolatorTime.FloatValue);
    }

    private void OnToggleInterpolator(bool obj)
    {
        if (InterpolatorToggle.BoolValue)
        {
            foreach (var player in CVRPlayerManager.Instance.NetworkPlayers)
            {
                OnPlayerEntityCreated(player);
            }
        }
        else
        {
            foreach (var pair in PlayerContainers)
            {
                pair.Destroy();
            }

            _selectedPlayerContainer = null;
            PlayerContainers.Clear();
        }
    }

    private void OnWorldLeave()
    {
        PlayerContainers.Clear();
    }

    public static void AnimatorFloatSetter(Animator animator, int paramNameHash, float paramValue)
    {
        if (_selectedPlayerContainer == null || !InterpolatorToggle.BoolValue || _selectedPlayerContainer.Animator != animator)
        {
            animator.SetFloat(paramNameHash, paramValue);
            return;
        }

        _selectedPlayerContainer.UpdateFloat(paramNameHash, paramValue);
    }

    private void OnPlayerEntityRecycled(CVRPlayerEntity player)
    {
        if (!GetContainerForPlayer(player, out var container)) return;

        _selectedPlayerContainer = null;
        container.Destroy();
        PlayerContainers.Remove(container);
    }

    private void OnPlayerEntityCreated(CVRPlayerEntity player)
    {
        if (GetContainerForPlayer(player, out _) || !InterpolatorToggle.BoolValue || _disabledPlayerIDs.Contains(player.Uuid)) return;

        PlayerContainers.Add(new PlayerContainer(player));
    }

    public void OnUpdate()
    {
        var envTick = (double)Environment.TickCount / 1000;

        //Local and server will be the same, I think this'll work?
        NetworkTickSystem.UpdateTick(envTick, envTick);
    }

    public static bool IsDefaultParam(string paramName)
    {
        return _defaultParams.Contains(paramName);
    }

    public static bool IsCommonOSCParam(string paramName)
    {
        return _commonFloatParams.Contains(paramName);
    }

    private void SaveDisabledParamInterpolation()
    {
        StringBuilder builder = new StringBuilder();
        foreach (string id in _disabledPlayerIDs)
        {
            builder.Append(id);
            builder.AppendLine();
        }
        File.WriteAllText("UserData\\BTKDisabledParamInterpolation.txt", builder.ToString());
    }

    private bool GetContainerForPlayer(CVRPlayerEntity player, out PlayerContainer container)
    {
        container = null;

        for (int i = 0; i < PlayerContainers.Count; i++)
        {
            if (PlayerContainers[i].Player == player) continue;

            container = PlayerContainers[i];
            return true;
        }

        return false;
    }
}