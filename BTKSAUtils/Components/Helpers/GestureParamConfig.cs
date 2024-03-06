using ABI_RC.Core.Savior;
using Newtonsoft.Json;

namespace BTKSAUtils.Components;

public class GestureParamConfig
{
    public bool Enabled { get; set; }
    public string Name => string.IsNullOrWhiteSpace(TargetParam) ? "New Param" : TargetParam;
    public string TargetParam { get; set; }
    public string LeftEmote { get; set; }
    public string RightEmote { get; set; }
    public bool HandsInView { get; set; }
    public bool CanReset { get; set; }
    public bool VibrateWhenTriggered { get; set; }
    public CVRGesture.GestureType GestureType { get; set; }
    public CVRGestureStep.GestureDirection GestureDirection { get; set; }

    [JsonIgnore]
    internal CVRGesture Gesture { get; set; }
    [JsonIgnore]
    internal CVRGestureStep GestureStep { get; set; }

    [JsonIgnore]
    internal bool ResetTriggered;
    [JsonIgnore]
    internal bool StartedOnStay;
    [JsonIgnore]
    internal DateTime LastResetHit;
}