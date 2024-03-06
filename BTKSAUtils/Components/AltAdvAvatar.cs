using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI.CCK.Scripts;
using BTKSAUtils.Config;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;

namespace BTKSAUtils.Components;

public class AltAdvAvatar
{
    public static AltAdvAvatar Instance;

    //Page data
    private Page _advAvatarRoot;
    private Category _sliderCat, _toggleCat, _numberEntryCat, _dropdownCat;
    private BTKBoolConfig _displayAdvAvaMenu = new(nameof(BTKSAUtils), "Display Alt Advanced Avatar Tab", "Enables or disables the alt advanced avatar tab", false, null, false);
    private Dictionary<string, Tuple<CVRAdvancedSettingsEntry.SettingsType, object>> _paramQMElements = new();

    internal void Init()
    {
        Instance = this;

        QuickMenuAPI.PrepareIcon("BTKAltAdvAva", "List", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.List.png"));
        QuickMenuAPI.PrepareIcon("BTKAltAdvAva", "Settings", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.Settings.png"));
        QuickMenuAPI.PrepareIcon("BTKAltAdvAva", "Body", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.Body.png"));

        _advAvatarRoot = new Page("BTKAltAdvAva", "AltAdvAvatar", true, "Body");
        _advAvatarRoot.MenuTitle = "Advanced Avatars";
        _advAvatarRoot.MenuSubtitle = "Control your advanced avatar parameters";

        _advAvatarRoot.HideTab = !_displayAdvAvaMenu.BoolValue;

        _displayAdvAvaMenu.OnConfigUpdated += o => { _advAvatarRoot.HideTab = !(bool)o; };

        Patches.OnLocalAvatarReady += AvatarSetupEvent;

        _toggleCat = _advAvatarRoot.AddCategory("Toggles");
        _dropdownCat = _advAvatarRoot.AddCategory("Dropdowns");
        _sliderCat = _advAvatarRoot.AddCategory("Sliders");
        _numberEntryCat = _advAvatarRoot.AddCategory("Single Entry");
    }

    public static void UpdateAvatarParam(string name, float value)
    {
        if (Instance == null || !Instance._paramQMElements.ContainsKey(name)) return;

        var element = Instance._paramQMElements[name];

        switch (element.Item1)
        {
            case CVRAdvancedSettingsEntry.SettingsType.GameObjectToggle:
                var toggle = (ToggleButton)element.Item2;
                toggle.ToggleValue = Math.Abs(value - 1f) < 0.9;
                break;
            case CVRAdvancedSettingsEntry.SettingsType.Slider:
                var slider = (SliderFloat)element.Item2;
                slider.SetSliderValue(value);
                break;
        }
    }

    private void AvatarSetupEvent()
    {
        //Reset page info
        _sliderCat.ClearChildren();
        _toggleCat.ClearChildren();
        _numberEntryCat.ClearChildren();
        _dropdownCat.ClearChildren();

        _paramQMElements.Clear();


        var avatarDescriptor = PlayerSetup.Instance.GetLocalAvatarDescriptor();
        var paramUIObjects = PlayerSetup.Instance.getCurrentAvatarSettings();

        foreach (var param in avatarDescriptor.avatarSettings.settings)
        {
            var uiObject = paramUIObjects.FirstOrDefault(x => x.parameterName.Equals(param.machineName));

            switch (param.type)
            {
                case CVRAdvancedSettingsEntry.SettingsType.GameObjectToggle:
                    var toggle = _toggleCat.AddToggle(uiObject.parameterName, $"Toggle state of {uiObject.parameterName} parameter", Math.Abs(uiObject.defaultValueX - 1) < 0.9);
                    toggle.OnValueUpdated += b =>
                    {
                        PlayerSetup.Instance.changeAnimatorParam(param.machineName, b ? 1f : 0f, 1);
                        CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(param.machineName, b ? 1f : 0f, false);
                    };

                    _paramQMElements.Add(param.machineName, new Tuple<CVRAdvancedSettingsEntry.SettingsType, object>(CVRAdvancedSettingsEntry.SettingsType.GameObjectToggle, toggle));
                    break;
                case CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown:
                    var multiSelect = new MultiSelection(uiObject.parameterName, uiObject.optionList, (int)uiObject.defaultValueX);
                    multiSelect.OnOptionUpdated += i =>
                    {
                        PlayerSetup.Instance.changeAnimatorParam(param.machineName, i, 1);
                        CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(param.machineName, i, false);
                    };
                    var msBtn = _dropdownCat.AddButton(uiObject.parameterName, "List", $"Open the multiselect page for {uiObject.parameterName}");
                    msBtn.OnPress += () => { QuickMenuAPI.OpenMultiSelect(multiSelect); };

                    _paramQMElements.Add(param.machineName, new Tuple<CVRAdvancedSettingsEntry.SettingsType, object>(CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown, multiSelect));
                    break;
                case CVRAdvancedSettingsEntry.SettingsType.Slider:
                    var slider = _sliderCat.AddSlider(uiObject.parameterName, $"Control the {uiObject.parameterName} slider", uiObject.defaultValueX, 0f, 1f, 2, uiObject.defaultValueX, true);
                    slider.OnValueUpdated += f =>
                    {
                        PlayerSetup.Instance.changeAnimatorParam(param.machineName, f, 1);
                        CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(param.machineName, f, false);
                    };

                    _paramQMElements.Add(param.machineName, new Tuple<CVRAdvancedSettingsEntry.SettingsType, object>(CVRAdvancedSettingsEntry.SettingsType.Slider, slider));
                    break;
                case CVRAdvancedSettingsEntry.SettingsType.InputSingle:
                    var singInput = _numberEntryCat.AddButton(uiObject.parameterName, "Settings", $"Opens the {uiObject.parameterName} single input page");
                    singInput.OnPress += () =>
                    {
                        QuickMenuAPI.OpenNumberInput(uiObject.parameterName, PlayerSetup.Instance.GetAnimatorParam(param.machineName), f =>
                        {
                            PlayerSetup.Instance.changeAnimatorParam(param.machineName, f, 1);
                            CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(param.machineName, f, false);
                        });
                    };
                    break;
            }
        }
    }
}