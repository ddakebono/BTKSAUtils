using System.Text;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Systems.GameEventSystem;
using BTKSAUtils.Components.Helpers;
using BTKSAUtils.Config;
using BTKUILib;
using MelonLoader;
using UnityEngine;

namespace BTKSAUtils.Components
{
    public class NameplateTweaks
    {
        public static NameplateTweaks Instance;
        public static BTKBoolConfig HideFriendNameplates = new(nameof(NameplateTweaks), "Hide Friends Nameplates", "This hides the nameplates of your friends but shows all others", false, null, false);
        public static BTKFloatConfig CloseRangeFadeMinDist = new(nameof(NameplateTweaks), "Close Range Distance Min", "Configure the minimum distance for close range fade, at this point the nameplate will be completely faded out", 0.75f, 0f, 5f, null, false);
        public static BTKFloatConfig CloseRangeFadeMaxDist = new(nameof(NameplateTweaks), "Close Range Distance Max", "Configure the maximum distance for close range fade, at this point the nameplate will be completely visible", 1.5f, 0f, 5f, null, false);
        public static List<PlayerNameplate> ActiveNameplates = new();

        private readonly List<string> _hiddenNameplateUserIDs = new();
        private static readonly int FadeStartDistance = Shader.PropertyToID("_FadeStartDistance");
        private static readonly int FadeEndDistance = Shader.PropertyToID("_FadeEndDistance");
        private Material _twImageMaterial;
        private Material _twTextMaterial;
        private Material _twPetAcceptMaterial;
        private Material _twMasterAcceptMaterial;

        public void Init()
        {
            Instance = this;
            
            Patches.OnNameplateRebuild += OnNameplateRebuild;

            HideFriendNameplates.OnConfigUpdated += OnConfigUpdated;
            CloseRangeFadeMinDist.OnConfigUpdated += ApplyFadeValues;
            CloseRangeFadeMaxDist.OnConfigUpdated += ApplyFadeValues;

            LoadHiddenNameplateFromFile();
        }

        private void OnWorldLeave(string _)
        {
            ActiveNameplates.Clear();
        }

        private void UserLeave(CVRPlayerEntity obj)
        {
            var nameplate = ActiveNameplates.FirstOrDefault(x => x.player.ownerId == obj.Uuid);
            if (nameplate != null)
                ActiveNameplates.Remove(nameplate);
        }

        public void LateInit()
        {
            CVRPlayerManager.Instance.OnPlayerEntityRecycled += UserLeave;
            CVRGameEventSystem.Instance.OnDisconnected.AddListener(OnWorldLeave);
            CVRGameEventSystem.Instance.OnConnectionLost.AddListener(OnWorldLeave);

            if (MelonMod.RegisteredMelons.Any(x => x.Info.Name.Equals("TotallyWholesome", StringComparison.InvariantCultureIgnoreCase)))
            {
                BTKSAUtils.Logger.Msg("TotallyWholesome found, grabbing nameplate status materials");
                var mats = TWNameplateAssetHelper.GetTWMaterials();

                _twImageMaterial = mats.Item1;
                _twTextMaterial = mats.Item2;
                _twPetAcceptMaterial = mats.Item3;
                _twMasterAcceptMaterial = mats.Item4;
            }

            ApplyFadeValues(0f);
        }

        public bool ToggleNameplateVisibility()
        {
            bool state = false;
            
            if (!_hiddenNameplateUserIDs.Contains(QuickMenuAPI.SelectedPlayerID))
            {
                _hiddenNameplateUserIDs.Add(QuickMenuAPI.SelectedPlayerID);
                state = true;
            }
            else
            {
                _hiddenNameplateUserIDs.Remove(QuickMenuAPI.SelectedPlayerID);
            }

            SaveHiddenNameplateFile();

            var nameplate = ActiveNameplates.FirstOrDefault(x => x.player.ownerId.Equals(QuickMenuAPI.SelectedPlayerID));
            if (nameplate == null) return state;

            nameplate.UpdateNamePlate();

            if (state)
                nameplate.s_Nameplate.SetActive(false);

            return state;
        }

        public bool IsNameplateHidden(string userID)
        {
            return _hiddenNameplateUserIDs.Contains(userID);
        }

        private void ApplyFadeValues(float _)
        {
            if(_twImageMaterial != null)
                ApplyMaterialProperties(_twImageMaterial);
            if(_twTextMaterial != null)
                ApplyMaterialProperties(_twTextMaterial);
            if(_twPetAcceptMaterial != null)
                ApplyMaterialProperties(_twPetAcceptMaterial);
            if(_twMasterAcceptMaterial != null)
                ApplyMaterialProperties(_twMasterAcceptMaterial);

            foreach (var nameplate in ActiveNameplates)
            {
                ApplyNameplateFade(nameplate);
            }
        }

        private void ApplyNameplateFade(PlayerNameplate nameplate)
        {
            ApplyMaterialProperties(nameplate.nameplateBackground.material);
            ApplyMaterialProperties(nameplate.usrNameText.fontMaterial);
            ApplyMaterialProperties(nameplate.rankText.fontMaterial);
            ApplyMaterialProperties(nameplate.playerImage.material);
            ApplyMaterialProperties(nameplate.staffplateBackground.material);
            ApplyMaterialProperties(nameplate.staffText.fontMaterial);
            ApplyMaterialProperties(nameplate.friendsImage.material);
        }

        private void ApplyMaterialProperties(Material mat)
        {
            mat.SetFloat(FadeStartDistance, CloseRangeFadeMinDist.FloatValue);
            mat.SetFloat(FadeEndDistance, CloseRangeFadeMaxDist.FloatValue);
        }

        private void OnConfigUpdated(bool _)
        {
            foreach (var nameplate in ActiveNameplates)
            {
                nameplate.UpdateNamePlate();

                if (Friends.FriendsWith(nameplate.player.ownerId) && HideFriendNameplates.BoolValue)
                    nameplate.s_Nameplate.SetActive(false);
            }
        }

        private void OnNameplateRebuild(PlayerNameplate obj)
        {
            if(!ActiveNameplates.Contains(obj))
                ActiveNameplates.Add(obj);

            if((Friends.FriendsWith(obj.player.ownerId) && HideFriendNameplates.BoolValue) || _hiddenNameplateUserIDs.Contains(obj.player.ownerId))
                obj.s_Nameplate.SetActive(false);

            ApplyNameplateFade(obj);
        }
        
        private void SaveHiddenNameplateFile()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string id in _hiddenNameplateUserIDs)
            {
                builder.Append(id);
                builder.AppendLine();
            }
            File.WriteAllText("UserData\\BTKHiddenNameplates.txt", builder.ToString());
        }

        private void LoadHiddenNameplateFromFile()
        {
            if (File.Exists("UserData\\BTKHiddenNameplates.txt"))
            {
                _hiddenNameplateUserIDs.Clear();

                string[] lines = File.ReadAllLines("UserData\\BTKHiddenNameplates.txt");

                foreach (string line in lines)
                {
                    if (!String.IsNullOrWhiteSpace(line))
                        _hiddenNameplateUserIDs.Add(line);
                }
            }
        }
    }
}