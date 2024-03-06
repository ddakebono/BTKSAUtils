using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI.CCK.Scripts;
using BTKSAUtils.Components;
using HarmonyLib;
using Yggdrasil.Logging;

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
    }

    private static void ApplyPatches(Type type)
    {
        try
        {
            BTKSAUtils.Harmony.PatchAll(type);
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
            Log.Error(e);
        }
    }
}