using System.Reflection;
using ABI_RC.Core.Player;
using ABI.CCK.Components;

namespace BTKSAUtils;

public static class Utils
{
    private static readonly FieldInfo LocalAvatarDescriptor = typeof(PlayerSetup).GetField("_avatarDescriptor", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo InternalCVRAvatarGetter = typeof(PuppetMaster).GetField("_avatar", BindingFlags.Instance | BindingFlags.NonPublic);

    public static CVRAvatar GetCVRAvatar(this PuppetMaster pm)
    {
        var avatar = InternalCVRAvatarGetter.GetValue(pm);

        return (CVRAvatar)avatar;
    }

    public static CVRAvatar GetLocalAvatarDescriptor(this PlayerSetup ps)
    {
        return (CVRAvatar)LocalAvatarDescriptor.GetValue(ps);
    }
}