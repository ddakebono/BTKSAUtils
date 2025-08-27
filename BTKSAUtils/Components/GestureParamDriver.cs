using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using Newtonsoft.Json;
using UnityEngine;

namespace BTKSAUtils.Components;

internal class GestureParamDriver
{
    internal static GestureParamDriver Instance;

    internal bool EnableGPD
    {
        get => _enabled;
        set
        {
            foreach (var param in GestureParams.Where(x => x.Enabled))
            {
                if (_enabled && !value)
                {
                    CVRGestureRecognizer.Instance.gestures.Remove(param.Gesture);
                    param.Gesture = null;
                    param.GestureStep = null;
                }

                if (!_enabled && value)
                {
                    ConfigureParamDriver(param, true);
                }
            }
            _enabled = value;
        }
    }
    internal Category ParamListCat { get; set; }

    //UI Stuff
    private Page _paramConfigPage;
    private ToggleButton _enabledToggle, _handsInView, _canReset, _vibrateWhenTriggered;
    private MultiSelection _emoteSelector, _paramSelector, _typeSelector, _directionSelector;
    private Dictionary<GestureParamConfig, Button> _paramListButtons = new();

    private List<GestureParamConfig> GestureParams { get; set; }
    private const string GestureParamStorage = "UserData\\BTKGestureParams.json";
    private bool _enabled;
    private GestureParamConfig _selectedConfig;
    private bool _rightEmoteSelector;

    public void LateInit()
    {
        Instance = this;

        //Load the params!
        GestureParams = File.Exists(GestureParamStorage) ? JsonConvert.DeserializeObject<List<GestureParamConfig>>(File.ReadAllText(GestureParamStorage)) : new();

        BTKSAUtils.Logger.Msg($"GestureParamDriver starting up, loading {GestureParams.Count} saved param driver configs!");

        //UI Setup
        _paramConfigPage = Page.GetOrCreatePage("BTKStandalone", "Gesture Param Config");
        var mainCat = _paramConfigPage.AddCategory("", false);
        _enabledToggle = mainCat.AddToggle("Enabled", "Enable or disable this gesture param driver", false);
        var saveButton = mainCat.AddButton("Save", "Checkmark", "Save changed to this config");
        saveButton.OnPress += SaveConfig;
        var deleteButton = mainCat.AddButton("Delete", "BTKSATrash", "Delete this config");
        deleteButton.OnPress += DeleteConfig;

        var toggleCat = _paramConfigPage.AddCategory("", false);
        _handsInView = toggleCat.AddToggle("Require Hands In View", "Only allows this gesture param to fire when both hands are in view", false);
        _canReset = toggleCat.AddToggle("Allow Reset/Toggle", "Allow this param to be reset to default or to toggle between on and off", false);
        _vibrateWhenTriggered = toggleCat.AddToggle("Vibrate When Triggered", "Vibrate your controllers when the gesture has been recognized", false);

        var selectorCat = _paramConfigPage.AddCategory("", false);
        var leftEmoteButton = selectorCat.AddButton("Left Emote", "Body", "Select which emote will be used in the left hand");
        leftEmoteButton.OnPress += () =>
        {
            _rightEmoteSelector = false;
            _emoteSelector.Name = "Left Emote";
            var index = Array.IndexOf(_emoteSelector.Options, _selectedConfig.LeftEmote);
            _emoteSelector.SelectedOption = index == -1 ? 0 : index;
            QuickMenuAPI.OpenMultiSelect(_emoteSelector);
        };
        var rightEmoteButton = selectorCat.AddButton("Right Emote", "Body", "Select which emote will be used in the right hand");
        rightEmoteButton.OnPress += () =>
        {
            _rightEmoteSelector = true;
            _emoteSelector.Name = "Right Emote";
            var index = Array.IndexOf(_emoteSelector.Options, _selectedConfig.RightEmote);
            _emoteSelector.SelectedOption = index == -1 ? 0 : index;
            QuickMenuAPI.OpenMultiSelect(_emoteSelector);
        };
        var paramSelector = selectorCat.AddButton("Select Parameter", "List", "Choose the parameter name from your avatars parameters");
        paramSelector.OnPress += SelectParam;
        var typeSelect = selectorCat.AddButton("Select Gesture Type", "List", "Set if this gesture is a one shot or held, held can be used to drive floats, one shot is more for bools");
        typeSelect.OnPress += () =>
        {
            _typeSelector.SelectedOption = (int)_selectedConfig.GestureType - 1;
            QuickMenuAPI.OpenMultiSelect(_typeSelector);
        };
        var directionSelect = selectorCat.AddButton("Select Gesture Direction", "List", "Set if this gesture should require movement and which way it moves");
        directionSelect.OnPress += () =>
        {
            _directionSelector.SelectedOption = (int)_selectedConfig.GestureDirection;
            QuickMenuAPI.OpenMultiSelect(_directionSelector);
        };

        //Setup multiselects
        _emoteSelector = new MultiSelection("Emote Selector", new[] { "Fist", "Open", "ThumbsUp", "Handgun", "Point", "Peace", "Rocknroll" }, 0);
        _emoteSelector.OnOptionUpdated += i =>
        {
            if (_selectedConfig == null)
                return;

            if (_rightEmoteSelector)
                _selectedConfig.RightEmote = _emoteSelector.Options[i];
            else
                _selectedConfig.LeftEmote = _emoteSelector.Options[i];
        };
        _paramSelector = new MultiSelection("Parameter Selection", new[] { "none" }, 0);
        _typeSelector = new MultiSelection("Gesture Type", Enum.GetNames(typeof(CVRGesture.GestureType)), 0);
        _directionSelector = new MultiSelection("Gesture Direction", Enum.GetNames(typeof(CVRGestureStep.GestureDirection)), 0);

        foreach (var param in GestureParams)
            ConfigureParamDriver(param, true);
    }

    public void AddNewParam()
    {
        OpenParamConfigPage(new GestureParamConfig());
    }

    private void SelectParam()
    {
        var paramUiObjects = PlayerSetup.Instance.getCurrentAvatarSettings();
        _paramSelector.Options = paramUiObjects.Select(x => x.parameterName).ToArray();
        _paramSelector.SelectedOption = _paramSelector.Options.Contains(_selectedConfig.TargetParam) ? Array.IndexOf(_paramSelector.Options, _selectedConfig.TargetParam) : 0;
        QuickMenuAPI.OpenMultiSelect(_paramSelector);
    }

    private void SaveConfig()
    {
        _selectedConfig.TargetParam = _paramSelector.Options[_paramSelector.SelectedOption];
        _selectedConfig.Enabled = _enabledToggle.ToggleValue;
        _selectedConfig.GestureType = (CVRGesture.GestureType)(_typeSelector.SelectedOption + 1);
        _selectedConfig.GestureDirection = (CVRGestureStep.GestureDirection)_directionSelector.SelectedOption;
        _selectedConfig.CanReset = _canReset.ToggleValue;
        _selectedConfig.HandsInView = _handsInView.ToggleValue;
        _selectedConfig.VibrateWhenTriggered = _vibrateWhenTriggered.ToggleValue;

        if(!GestureParams.Contains(_selectedConfig))
            GestureParams.Add(_selectedConfig);

        ConfigureParamDriver(_selectedConfig);
        QuickMenuAPI.GoBack();
    }

    private void DeleteConfig()
    {
        if (_selectedConfig.Gesture != null)
        {
            CVRGestureRecognizer.Instance.gestures.Remove(_selectedConfig.Gesture);
            _selectedConfig.Gesture = null;
            _selectedConfig.GestureStep = null;
        }

        if (_paramListButtons.ContainsKey(_selectedConfig))
        {
            _paramListButtons[_selectedConfig].Delete();
            _paramListButtons.Remove(_selectedConfig);
        }

        GestureParams.Remove(_selectedConfig);

        File.WriteAllText(GestureParamStorage, JsonConvert.SerializeObject(GestureParams));

        QuickMenuAPI.GoBack();
    }

    private void OpenParamConfigPage(GestureParamConfig config)
    {
        _selectedConfig = config;

        _paramConfigPage.PageDisplayName = $"Editing {config.Name}";

        _enabledToggle.ToggleValue = config.Enabled;
        _canReset.ToggleValue = config.CanReset;
        _vibrateWhenTriggered.ToggleValue = config.VibrateWhenTriggered;
        _handsInView.ToggleValue = config.HandsInView;

        _paramConfigPage.OpenPage();
    }

    private void ConfigureParamDriver(GestureParamConfig config, bool dontSave = false)
    {
        if (!dontSave)
        {
            //Save changes to params
            File.WriteAllText(GestureParamStorage, JsonConvert.SerializeObject(GestureParams));
        }

        if (!_paramListButtons.ContainsKey(config))
        {
            var newButton = ParamListCat.AddButton($"Edit {config.Name}", "Settings", $"Edit the config for {config.Name}");
            newButton.OnPress += () =>
            {
                OpenParamConfigPage(config);
            };

            _paramListButtons.Add(config, newButton);
        }
        else
        {
            _paramListButtons[config].ButtonText = config.Name;
        }

        if (config.Gesture != null)
        {
            //Update CVRGesture
            config.Gesture.steps.Clear();

            if (!config.Enabled)
            {
                //Param was disabled
                CVRGestureRecognizer.Instance.gestures.Remove(config.Gesture);
                config.Gesture = null;
                config.GestureStep = null;
                return;
            }
        }
        else
        {
            if (!config.Enabled)
                return;

            var uuid = Guid.NewGuid();
            config.Gesture = new CVRGesture();
            config.Gesture.name = $"BTKGestureDriver-{uuid}";

            config.Gesture.onStart.AddListener((_, _, _) =>
            {
                if (config.GestureType == CVRGesture.GestureType.OneShot)
                {
                    if (DateTime.Now.Subtract(config.LastResetHit).TotalSeconds >= .5)
                    {
                        var animParam = PlayerSetup.Instance.AnimatorManager.Animator.parameters.FirstOrDefault(x => x.name == config.TargetParam);

                        if (animParam == null)
                            return;

                        if (animParam.type != AnimatorControllerParameterType.Bool)
                        {
                            BTKSAUtils.Logger.Msg($"GestureParamDriver for {config.Name} is set to OneShot mode! This can only be used for Bool parameters!");
                            return;
                        }

                        if (config.VibrateWhenTriggered)
                        {
                            CVRInputManager.Instance.Vibrate(0f, 0.1f, 10f, 1f, CVRHand.Left);
                            CVRInputManager.Instance.Vibrate(0f, 0.1f, 10f, 1f, CVRHand.Right);
                            CVRInputManager.Instance.Vibrate(0.2f, 0.1f, 10f, 1f, CVRHand.Left);
                            CVRInputManager.Instance.Vibrate(0.2f, 0.1f, 10f, 1f, CVRHand.Right);
                        }

                        switch (animParam.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                //Check if this is a oneshot with no reset and the parameter is already true
                                var currentState = PlayerSetup.Instance.AnimatorManager.Animator.GetBool(animParam.nameHash);
                                if (config.GestureType == CVRGesture.GestureType.OneShot && currentState && !config.CanReset)
                                    return;

                                PlayerSetup.Instance.AnimatorManager.SetParameter(config.TargetParam, !currentState);
                                CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(config.TargetParam, !currentState ? 1 : 0, false);
                                break;
                        }
                    }

                    config.LastResetHit = DateTime.Now;

                    return;
                }

                config.ResetTriggered = false;
                config.StartedOnStay = false;

                if (DateTime.Now.Subtract(config.LastResetHit).TotalSeconds >= .8 && config.CanReset)
                {
                    //Reset the parameter to default
                    var animParam = PlayerSetup.Instance.AnimatorManager.Animator.parameters.FirstOrDefault(x => x.name == config.TargetParam);

                    if (animParam == null) return;

                    config.ResetTriggered = true;

                    if (config.VibrateWhenTriggered)
                    {
                        CVRInputManager.Instance.Vibrate(0f, 0.1f, 10f, 1f, CVRHand.Left);
                        CVRInputManager.Instance.Vibrate(0f, 0.1f, 10f, 1f, CVRHand.Right);
                        CVRInputManager.Instance.Vibrate(0.2f, 0.1f, 10f, 1f, CVRHand.Left);
                        CVRInputManager.Instance.Vibrate(0.2f, 0.1f, 10f, 1f, CVRHand.Right);
                    }

                    switch (animParam.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            PlayerSetup.Instance.AnimatorManager.SetParameter(config.TargetParam, animParam.defaultFloat);
                            CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(config.TargetParam, animParam.defaultFloat, false);
                            break;
                        case AnimatorControllerParameterType.Int:
                            PlayerSetup.Instance.AnimatorManager.SetParameter(config.TargetParam, animParam.defaultInt);
                            CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(config.TargetParam, animParam.defaultInt, false);
                            break;
                        case AnimatorControllerParameterType.Bool:
                            PlayerSetup.Instance.AnimatorManager.SetParameter(config.TargetParam, animParam.defaultBool ? 1 : 0);
                            CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(config.TargetParam, animParam.defaultBool ? 1 : 0, false);
                            break;
                    }
                }

                config.LastResetHit = DateTime.Now;
            });

            config.Gesture.onStay.AddListener((fl, _, _) =>
            {
                if (!config.Enabled || config.ResetTriggered) return;

                if (!config.StartedOnStay)
                {
                    config.StartedOnStay = true;

                    if (config.VibrateWhenTriggered)
                    {
                        CVRInputManager.Instance.Vibrate(0f, 0.2f, 20f, 1f, CVRHand.Left);
                        CVRInputManager.Instance.Vibrate(0f, 0.2f, 20f, 1f, CVRHand.Right);
                    }
                }

                PlayerSetup.Instance.AnimatorManager.SetParameter(config.TargetParam, fl);
                CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(config.TargetParam, fl, false);
            });
        }

        config.Gesture.type = config.GestureType;

        config.GestureStep = new CVRGestureStep
        {
            firstGesture = GetValidGesture(config.RightEmote),
            secondGesture = GetValidGesture(config.LeftEmote),
            needsToBeInView = config.HandsInView,
            direction = config.GestureDirection,
            startDistance = .25f,
            endDistance = 1000f,
            maxRelativeDirection = 1f
        };

        config.Gesture.steps.Add(config.GestureStep);

        if(!CVRGestureRecognizer.Instance.gestures.Contains(config.Gesture))
            CVRGestureRecognizer.Instance.gestures.Add(config.Gesture);
    }

    private CVRGestureStep.Gesture GetValidGesture(string gesture)
    {
        if (!Enum.TryParse(gesture, true, out CVRGestureStep.Gesture result))
        {
            BTKSAUtils.Logger.Error("The selected gesture does not exist! Defaulting to Fist");
        }

        return result;
    }
}