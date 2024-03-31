using System.Collections;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI.CCK.Components;
using BTKSAUtils.Components.AvatarParamInterpolator.Interpolation;
using MelonLoader;
using UnityEngine;

namespace BTKSAUtils.Components.AvatarParamInterpolator;

public class PlayerContainer
{
    public readonly CVRPlayerEntity Player;
    public CVRAnimatorManager Cam;

    private readonly Dictionary<string, BufferedLinearInterpolatorFloat> _interpolatedFloats = new();
    private object _coroutineToken;
    private bool _destroy;
    private bool _outOfRange;

    public PlayerContainer(CVRPlayerEntity player)
    {
        Player = player;

        Player.PuppetMaster.OnAvatarInstantiated += OnAvatarInstantiated;

        var avatar = Player.PuppetMaster.GetCVRAvatar();

        if (avatar != null)
        {
            OnAvatarInstantiated(Player.PuppetMaster.avatarObject, avatar);
        }

        _coroutineToken = MelonCoroutines.Start(FloatParamUpdateCoroutine());
    }

    public void UpdateFloat(string paramName, float targetValue)
    {
        if (!_interpolatedFloats.ContainsKey(paramName) || _outOfRange)
        {
            Cam.SetAnimatorParameterFloat(paramName, targetValue);
            return;
        }

        var interpolator = _interpolatedFloats[paramName];
        interpolator.AddMeasurement(targetValue, AvatarParamInterpolator.NetworkTickSystem.LocalTime.Time);
    }

    public void Destroy()
    {
        _destroy = true;
        //Unregister our listener when the object is destroyed
        Player.PuppetMaster.OnAvatarInstantiated -= OnAvatarInstantiated;
        MelonCoroutines.Stop(_coroutineToken);
    }

    public void UpdateInterpolatorTime(float time)
    {
        foreach (var interpolator in _interpolatedFloats.Values)
            interpolator.MaximumInterpolationTime = time;
    }

    public void ReapplyParamSetup()
    {
        var avatar = Player.PuppetMaster.GetCVRAvatar();
        OnAvatarInstantiated(Player.PuppetMaster.avatarObject, avatar);
    }

    private IEnumerator FloatParamUpdateCoroutine()
    {
        while (!_destroy)
        {
            yield return null;

            if(_interpolatedFloats.Count == 0) continue;

            _outOfRange = Vector3.Distance(Player.AvatarHolder.transform.position, PlayerSetup.Instance.gameObject.transform.position) >= AvatarParamInterpolator.MaxInterpolationDistance.FloatValue;

            if (_outOfRange)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            var renderTime = AvatarParamInterpolator.NetworkTickSystem.LocalTime.TimeTicksAgo(1).Time;
            var localTime = AvatarParamInterpolator.NetworkTickSystem.LocalTime.Time;

            foreach (var pair in _interpolatedFloats)
            {
                pair.Value.Update(Time.deltaTime, renderTime, localTime);
                var value = pair.Value.GetInterpolatedValue();

                Cam.SetAnimatorParameterFloat(pair.Key, value);
            }
        }
    }

    private void OnAvatarInstantiated(GameObject goRoot, CVRAvatar avatar)
    {
        _interpolatedFloats.Clear();

        Cam = Player.PuppetMaster.animatorManager;

        var floatParams = Player.PuppetMaster.animatorManager.animator.parameters.Where(x => x.type == AnimatorControllerParameterType.Float).ToArray();

        foreach (var param in floatParams.Where(x => !AvatarParamInterpolator.IsDefaultParam(x.name) && !x.name.StartsWith("#") && (!AvatarParamInterpolator.CommonOSCOnly.BoolValue || AvatarParamInterpolator.IsCommonOSCParam(x.name))))
        {
            var interpolator = new BufferedLinearInterpolatorFloat();
            interpolator.MaximumInterpolationTime = AvatarParamInterpolator.InterpolatorTime.FloatValue;

            interpolator.ResetTo(param.defaultFloat, AvatarParamInterpolator.NetworkTickSystem.LocalTime.Time);

            _interpolatedFloats.Add(param.name, interpolator);
        }
    }
}