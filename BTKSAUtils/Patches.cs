using System.Reflection;
using System.Reflection.Emit;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util.AnimatorManager;
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
    [HarmonyPatch(nameof(PlayerSetup.changeAnimatorParam))]
    [HarmonyPostfix]
    static void SetAnimParamPostfix(string name, float value)
    {
        AltAdvAvatar.UpdateAvatarParam(name, value);
    }
}

[HarmonyPatch(typeof(PlayerNameplate))]
class NameplatePatches
{
    [HarmonyPatch(nameof(PlayerNameplate.UpdateNamePlate))]
    [HarmonyPostfix]
    static void UpdateNameplate(PlayerNameplate __instance)
    {
        try
        {
            Patches.OnNameplateRebuild?.Invoke(__instance);
        }
        catch (Exception e)
        {
            BTKSAUtils.Logger.Error(e);
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