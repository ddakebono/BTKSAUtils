using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using BTKSAUtils.Components;
using BTKSAUtils.Config;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using Semver;

namespace BTKSAUtils;

public static class BuildInfo
{
    public const string Name = "BTKSAUtils"; // Name of the Mod.  (MUST BE SET)
    public const string Author = "DDAkebono"; // Author of the Mod.  (Set as null if none)
    public const string Company = "BTK-Development"; // Company that made the Mod.  (Set as null if none)
    public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
    public const string DownloadLink = "https://github.com/ddakebono/BTKSAUtils/releases"; // Download Link for the Mod.  (Set as null if none)
}

public class BTKSAUtils : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal static HarmonyLib.Harmony Harmony;
    internal static readonly List<BTKBaseConfig> BTKConfigs = new();

    private bool _hasSetupUI;
    private AltAdvAvatar _altAdvAvatar = new();
    private GestureParamDriver _gestureParamDriver = new();
    private NameplateTweaks _nameplateTweaks = new();

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Harmony = HarmonyInstance;

        Logger.Msg("BTK Standalone: Nameplate Mod - Starting up");

        if (RegisteredMelons.Any(x => x.Info.Name.Equals("BTKCompanionLoader", StringComparison.OrdinalIgnoreCase)))
        {
            MelonLogger.Msg("Hold on a sec! Looks like you've got BTKCompanion installed, this mod is built in and not needed!");
            MelonLogger.Error("BTKSAUtils has not started up! (BTKCompanion Running)");
            return;
        }

        if (!RegisteredMelons.Any(x => x.Info.Name.Equals("BTKUILib") && x.Info.SemanticVersion != null && x.Info.SemanticVersion.CompareTo(new SemVersion(1)) >= 0))
        {
            Logger.Error("BTKUILib was not detected or it outdated! BTKSAUtils cannot function without it!");
            Logger.Error("Please download an updated copy for BTKUILib!");
            return;
        }

        _altAdvAvatar.Init();
        _nameplateTweaks.Init();
        Patches.SetupPatches();

        QuickMenuAPI.OnMenuRegenerate += SetupUI;
    }

    private void SetupUI(CVR_MenuManager _)
    {
        if(_hasSetupUI) return;
        _hasSetupUI = true;

        QuickMenuAPI.PrepareIcon("BTKStandalone", "BTKIcon", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.BTKIcon.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "Settings", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.Settings.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "TurnOff", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.TurnOff.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "List", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.List.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "Checkmark", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.Checkmark.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "BTKSATrash", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.BTKSATrash.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "Body", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.Body.png"));
        QuickMenuAPI.PrepareIcon("BTKStandalone", "Star", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKSAUtils.Images.Star.png"));

        Page rootPage = Page.GetOrCreatePage("BTKStandalone", "MainPage", true, "BTKIcon");

        rootPage.MenuTitle = "BTK Standalone Mods";
        rootPage.MenuSubtitle = "Toggle and configure your BTK Standalone mods here!";

        var functionToggles = rootPage.AddCategory("Bono's Utils");

        var gpdToggle = functionToggles.AddToggle("Enable Gesture Param Drivers", "Toggles the entire gesture param driver on or off", true);
        gpdToggle.OnValueUpdated += b =>
        {
            GestureParamDriver.Instance.EnableGPD = b;
        };

        var settingsPage = functionToggles.AddPage("Util Settings", "Settings", "Change and configure all parts of Bono's Utils", "BTKStandalone");

        var configCategories = new Dictionary<string, Category>();

        foreach (var config in BTKConfigs)
        {
            if (!configCategories.ContainsKey(config.Category))
                configCategories.Add(config.Category, settingsPage.AddCategory(config.Category));

            var cat = configCategories[config.Category];

            switch (config.Type)
            {
                case { } boolType when boolType == typeof(bool):
                    ToggleButton toggle = null;
                    var boolConfig = (BTKBoolConfig)config;
                    toggle = cat.AddToggle(config.Name, config.Description, boolConfig.BoolValue);
                    toggle.OnValueUpdated += b =>
                    {
                        if (!ConfigDialogs(config))
                            toggle.ToggleValue = boolConfig.BoolValue;

                        boolConfig.BoolValue = b;
                    };
                    break;
                case {} floatType when floatType == typeof(float):
                    SliderFloat slider = null;
                    var floatConfig = (BTKFloatConfig)config;
                    slider = cat.AddSlider(floatConfig.Name, floatConfig.Description, Convert.ToSingle(floatConfig.FloatValue), floatConfig.MinValue, floatConfig.MaxValue);
                    slider.OnValueUpdated += f =>
                    {
                        if (!ConfigDialogs(config))
                        {
                            slider.SetSliderValue(floatConfig.FloatValue);
                            return;
                        }

                        floatConfig.FloatValue = f;

                    };
                    break;
            }
        }

        //Setup GPD UI
        var utilsCat = configCategories[nameof(BTKSAUtils)];

        var gpdPage = utilsCat.AddPage("Gesture Param Driver Config", "List", "Add, remove, and configure your gesture param drivers!", "BTKStandalone");
        _gestureParamDriver.ParamListCat = gpdPage.AddCategory("Saved Configs");
        var addButton = _gestureParamDriver.ParamListCat.AddButton("Add New Config", "Star", "Add a new config");
        addButton.OnPress += _gestureParamDriver.AddNewParam;


        _gestureParamDriver.LateInit();
        _nameplateTweaks.LateInit();

        Logger.Msg("Bono's Utils are ready to use!");
    }

    private bool ConfigDialogs(BTKBaseConfig config)
    {
        if (config.DialogMessage != null)
        {
            QuickMenuAPI.ShowNotice("Notice", config.DialogMessage);
        }

        return true;
    }
}