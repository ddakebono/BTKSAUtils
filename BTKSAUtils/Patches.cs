using System.Reflection;
using System.Reflection.Emit;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI.CCK.Scripts;
using BTKSAUtils.Components;
using BTKSAUtils.Components.AvatarParamInterpolator;
using HarmonyLib;

namespace BTKSAUtils;

internal static class Patches
{
    public static Action OnLocalAvatarReady;
    public static Action<PlayerNameplate> OnNameplateRebuild;

    public static void SetupPatches()
    {
        ApplyPatches(typeof(AdvancedAvatarSettingsPatch));
        ApplyPatches(typeof(AnimatorManagerPatches));
        ApplyPatches(typeof(NameplatePatches));
        ApplyPatches(typeof(AnimatorManagerPatch));
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

[HarmonyPatch(typeof(CVRAnimatorManager))]
class AnimatorManagerPatches
{
    [HarmonyPatch(nameof(CVRAnimatorManager.SetAnimatorParameter))]
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
    private static readonly MethodInfo SetAnimFloatMethod = typeof(CVRAnimatorManager).GetMethod(nameof(CVRAnimatorManager.SetAnimatorParameterFloat), BindingFlags.Instance | BindingFlags.Public);
    private static readonly MethodInfo OurAnimatorFloatSetter = typeof(AvatarParamInterpolator).GetMethod(nameof(AvatarParamInterpolator.AnimatorFloatSetter), BindingFlags.Static | BindingFlags.Public);

    static MethodBase TargetMethod()
    {
        return typeof(CVRAnimatorManager).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(x => x.Name == nameof(CVRAnimatorManager.ApplyAdvancedAvatarSettings) && x.GetParameters().Any(p => p.ParameterType == typeof(bool[])));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Call, SetAnimFloatMethod))
            .SetOperandAndAdvance(OurAnimatorFloatSetter)
            .InstructionEnumeration();
    }
}