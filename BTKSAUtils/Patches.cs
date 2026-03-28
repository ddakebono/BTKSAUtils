using System.Reflection;
using System.Reflection.Emit;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util.AnimatorManager;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Scripts;
using BTKSAUtils.Components;
using BTKSAUtils.Components.AvatarParamInterpolator;
using HarmonyLib;
using UnityEngine;

namespace BTKSAUtils;

internal static class Patches
{
    public static Action OnLocalAvatarReady;
    public static Action<PlayerNameplate> OnNameplateRebuild;

    public static void SetupPatches()
    {
        ApplyPatches(typeof(AdvancedAvatarSettingsPatch));
        ApplyPatches(typeof(PlayerSetupPatch));
        ApplyPatches(typeof(NameplatePatches));
        ApplyPatches(typeof(AnimatorManagerPatch));
        ApplyPatches(typeof(AvatarAnimManagerPatch));

        CVRGameEventSystem.Avatar.OnRemoteAvatarLoad.AddListener((entity, avatar) =>
        {
            //Fire avatar load in PlayerContainer
            if(!AvatarParamInterpolator.GetContainerForPlayer(entity, out var container)) return;

            container.OnAvatarInstantiated();
        });
    }

    private static void ApplyPatches(Type type)
    {
        try
        {
            BTKSAUtils.HarmonyInst.PatchAll(type);
        }
        catch(Exception e)
        {
            BTKSAUtils.Logger.Msg($"Failed while patching {type.Name}!");
            BTKSAUtils.Logger.Error(e);
        }
    }
}

[HarmonyPatch(typeof(CVRAdvancedAvatarSettings))]
class AdvancedAvatarSettingsPatch
{
    [HarmonyPatch(nameof(CVRAdvancedAvatarSettings.LoadAvatarProfiles))]
    [HarmonyPostfix]
    static void OnLoadAvatarProfiles()
    {
        try
        {
            Patches.OnLocalAvatarReady?.Invoke();
        }
        catch (Exception e)
        {
            BTKSAUtils.Logger.Error(e);
        }
    }
}

[HarmonyPatch(typeof(PlayerSetup))]
class PlayerSetupPatch
{
    [HarmonyPatch(nameof(PlayerSetup.ChangeAnimatorParam))]
    [HarmonyPostfix]
    static void SetAnimParamPostfix(string parameterName, float value)
    {
        AltAdvAvatar.UpdateAvatarParam(parameterName, value);
    }
}

[HarmonyPatch(typeof(PlayerNameplate))]
class NameplatePatches
{
    private static MethodInfo _isActivePrivSetter = typeof(PlayerNameplate).GetProperty("IsActive", BindingFlags.NonPublic | BindingFlags.Instance)?.SetMethod;
    private static object[] _paramArray = [false];
    
    [HarmonyPatch(nameof(PlayerNameplate.UpdateNamePlateSettings))]
    [HarmonyPostfix]
    static void UpdateNameplate(PlayerNameplate __instance)
    {
        //Don't check local player nameplate
        if (__instance.Player.IsLocalPlayer) return;
        
        try
        {
            Patches.OnNameplateRebuild?.Invoke(__instance);
        }
        catch (Exception e)
        {
            BTKSAUtils.Logger.Error(e);
        }
    }

    [HarmonyPatch("UpdateNamePlateState")]
    [HarmonyPostfix]
    static void UpdateNameplateState(PlayerNameplate __instance)
    {
        //Don't check local player nameplate
        if (__instance.Player.IsLocalPlayer) return;
        
        string userId = __instance.PlayerDescriptor.ownerId;

        if (NameplateTweaks.HiddenNameplateUserIDs.Contains(userId) ||
            (NameplateTweaks.HideFriendNameplates.BoolValue && Friends.FriendsWith(userId)))
        {
            //Set nameplate to false if it's hidden on friend nameplates are hidden and they're a friend
            _isActivePrivSetter.Invoke(__instance, _paramArray);
        }
    }
}

[HarmonyPatch]
class AnimatorManagerPatch
{
    private static MethodInfo _setAnimFloatMethod = typeof(Animator).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(x => x.Name == "SetFloat" && x.GetParameters().Any(p => p.ParameterType == typeof(int)) && x.GetParameters().Length == 2);
    private static MethodInfo _ourAnimatorFloatSetter = typeof(AvatarParamInterpolator).GetMethod(nameof(AvatarParamInterpolator.AnimatorFloatSetter), BindingFlags.Static | BindingFlags.Public);

    static MethodBase TargetMethod()
    {
        var func = typeof(AvatarAnimatorManager).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == "ApplyAdvancedAvatarSettings" && x.GetParameters().Any(p => p.ParameterType == typeof(bool[])));

        return func;
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _setAnimFloatMethod))
            .SetOperandAndAdvance(_ourAnimatorFloatSetter)
            .InstructionEnumeration();
    }
}

[HarmonyPatch(typeof(AvatarAnimatorManager))]
class AvatarAnimManagerPatch
{
    [HarmonyPatch("ApplyAdvancedAvatarSettings", typeof(float[]), typeof(int[]), typeof(bool[]), typeof(bool))]
    [HarmonyPrefix]
    static bool ApplyPrefix(AvatarAnimatorManager __instance)
    {
        if (__instance.Animator == null || !AvatarParamInterpolator.InterpolatorToggle.BoolValue) return true;

        AvatarParamInterpolator.OnApplyAdvAvatarSettingsPrefix(__instance.Animator);

        return true;
    }
}