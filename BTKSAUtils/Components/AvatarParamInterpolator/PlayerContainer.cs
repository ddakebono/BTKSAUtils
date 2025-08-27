using System.Collections;
using ABI_RC.Core.Player;
using ABI.CCK.Components;
using BTKSAUtils.Components.AvatarParamInterpolator.Interpolation;
using MelonLoader;
using UnityEngine;

namespace BTKSAUtils.Components.AvatarParamInterpolator;

public class PlayerContainer
{
    public readonly CVRPlayerEntity Player;
    public Animator Animator;

    private readonly Dictionary<int, BufferedLinearInterpolatorFloat> _interpolatedFloats = new();
    private object _coroutineToken;
    private bool _destroy;
    private bool _outOfRange;

    public PlayerContainer(CVRPlayerEntity player)
    {
        Player = player;

        var avatar = Player.PuppetMaster.AvatarDescriptor;

        if (avatar != null)
        {
            OnAvatarInstantiated();
        }

        _coroutineToken = MelonCoroutines.Start(FloatParamUpdateCoroutine());
    }

    public void UpdateFloat(int paramNameHash, float targetValue)
    {
        if (!_interpolatedFloats.ContainsKey(paramNameHash) || _outOfRange)
        {
            Animator.SetFloat(paramNameHash, targetValue);
            return;
        }

        var interpolator = _interpolatedFloats[paramNameHash];
        interpolator.AddMeasurement(targetValue, AvatarParamInterpolator.NetworkTickSystem.LocalTime.Time);
    }

    public void Destroy()
    {
        _destroy = true;
        //Unregister our listener when the object is destroyed
        _interpolatedFloats.Clear();
        MelonCoroutines.Stop(_coroutineToken);
    }

    public void UpdateInterpolatorTime(float time)
    {
        foreach (var interpolator in _interpolatedFloats.Values)
            interpolator.MaximumInterpolationTime = time;
    }

    public void ReapplyParamSetup()
    {
        OnAvatarInstantiated();
    }

    private IEnumerator FloatParamUpdateCoroutine()
    {
        while (!_destroy)
        {
            yield return null;

            if(_interpolatedFloats.Count == 0) continue;

            _outOfRange = Vector3.Distance(Player.AvatarHolder.transform.position, PlayerSetup.Instance.gameObject.transform.position) >= AvatarParamInterpolator.MaxInterpolationDistance.FloatValue;

            if (_outOfRange || Animator == null)
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

                Animator.SetFloat(pair.Key, value);
            }
        }
    }

    public void OnAvatarInstantiated()
    {
        _interpolatedFloats.Clear();

        Animator = Player.PuppetMaster.AnimatorManager.Animator;

        var floatParams = Player.PuppetMaster.AnimatorManager.Animator.parameters.Where(x => x.type == AnimatorControllerParameterType.Float).ToArray();

        foreach (var param in floatParams.Where(x => !AvatarParamInterpolator.IsDefaultParam(x.name) && !x.name.StartsWith("#") && (!AvatarParamInterpolator.CommonOSCOnly.BoolValue || AvatarParamInterpolator.IsCommonOSCParam(x.name))))
        {
            var interpolator = new BufferedLinearInterpolatorFloat();
            interpolator.MaximumInterpolationTime = AvatarParamInterpolator.InterpolatorTime.FloatValue;

            interpolator.ResetTo(Animator.GetFloat(param.nameHash), AvatarParamInterpolator.NetworkTickSystem.LocalTime.Time);

            _interpolatedFloats.Add(param.nameHash, interpolator);
        }
    }
}