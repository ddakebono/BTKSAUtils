using System.Reflection;
using ABI_RC.Core.Player;
using ABI.CCK.Components;

namespace BTKSAUtils;

public static class Utils
{
    private static FieldInfo _localAvatarDescriptor = typeof(PlayerSetup).GetField("_avatarDescriptor", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static CVRAvatar GetLocalAvatarDescriptor(this PlayerSetup ps)
    {
        return (CVRAvatar)_localAvatarDescriptor.GetValue(ps);
    }
}