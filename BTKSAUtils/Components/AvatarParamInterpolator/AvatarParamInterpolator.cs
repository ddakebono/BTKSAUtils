using System.Text;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using BTKSAUtils.Components.AvatarParamInterpolator.Interpolation;
using BTKSAUtils.Config;
using BTKUILib;

namespace BTKSAUtils.Components.AvatarParamInterpolator;

public class AvatarParamInterpolator
{
    public static NetworkTickSystem NetworkTickSystem;
    public readonly BTKBoolConfig InterpolatorToggle = new("Param Interpolation", "Enable Param Interpolation", "Turns the interpolator on and off", true, null, false);
    public static readonly BTKFloatConfig InterpolatorTime = new("Param Interpolation", "Interpolation Time", "This controls how long it takes for the interpolated value to reach the target", 0.05f, 0f, 1f, null, false);
    public static readonly BTKFloatConfig MaxInterpolationDistance = new("Param Interpolation", "Max Interpolation Distance", "Set how far away a player must be before interpolation stops being used", 8f, 0f, 20f, null, false);

    private static readonly Dictionary<CVRAnimatorManager, PlayerContainer> PlayerContainers = new();
    private readonly List<string> _disabledPlayerIDs = new();

    private static string[] _defaultParams =
    {
        "MovementX", "MovementY", "Grounded", "Emote", "GestureLeft", "GestureRight", "Toggle", "Sitting", "Crouching", "CancelEmote", "Prone", "Flying"
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

    private void OnPlayerSelectDisable(bool state)
    {
        if (state)
        {
            var playerContainer = PlayerContainers.Values.FirstOrDefault(x => x.Player.Uuid == QuickMenuAPI.SelectedPlayerID);

            if (playerContainer == null) return;

            playerContainer.Destroy();
            PlayerContainers.Remove(playerContainer.Cam);
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
        foreach(var player in PlayerContainers.Values)
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
                pair.Value.Destroy();
            }

            PlayerContainers.Clear();
        }
    }

    private void OnWorldLeave()
    {
        PlayerContainers.Clear();
    }

    public static void AnimatorFloatSetter(CVRAnimatorManager cam, string paramName, float paramValue)
    {
        //Hijacked from the ApplyAdvancedAvatarSettings function
        if (!PlayerContainers.ContainsKey(cam))
        {
            cam.SetAnimatorParameterFloat(paramName, paramValue);
            return;
        }

        PlayerContainers[cam].UpdateFloat(paramName, paramValue);
    }

    private void OnPlayerEntityRecycled(CVRPlayerEntity player)
    {
        if (!PlayerContainers.ContainsKey(player.PuppetMaster.animatorManager)) return;

        PlayerContainers[player.PuppetMaster.animatorManager].Destroy();
        PlayerContainers.Remove(player.PuppetMaster.animatorManager);
    }

    private void OnPlayerEntityCreated(CVRPlayerEntity player)
    {
        if (PlayerContainers.ContainsKey(player.PuppetMaster.animatorManager) || !InterpolatorToggle.BoolValue || _disabledPlayerIDs.Contains(player.Uuid)) return;

        PlayerContainers.Add(player.PuppetMaster.animatorManager, new PlayerContainer(player));
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
}