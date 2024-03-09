using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTKSAUtils.Components.Helpers;

public class TWNameplateAssetHelper
{
    public static (Material, Material, Material, Material) GetTWMaterials()
    {
        Type twAssetsType = Type.GetType("TotallyWholesome.TWAssets, TotallyWholesome");
        if (twAssetsType == null)
        {
            BTKSAUtils.Logger.Warning("TotallyWholesome was detected but the TWAssets class was not found! Unable to control TW nameplate status fade!");
            return (null, null, null, null);
        }
        var statusField = twAssetsType.GetField("StatusPrefab", BindingFlags.Static | BindingFlags.Public);
        if (statusField == null)
        {
            BTKSAUtils.Logger.Warning("TotallyWholesome was detected but the StatusPrefab was not found! Unable to control TW nameplate status fade!");
            return (null, null, null, null);
        }

        GameObject prefab = statusField.GetValue(twAssetsType) as GameObject;

        if (prefab == null)
        {
            BTKSAUtils.Logger.Warning("TotallyWholesome was detected but the StatusPrefab was invalid! Unable to control TW nameplate status fade!");
            return (null, null, null, null);
        }

        var imageAsset = prefab.transform.Find("SpecialMark").GetComponent<Image>();
        var textAsset = imageAsset.transform.Find("SpecialMarkText").GetComponent<TextMeshProUGUI>();
        var petAutoAsset = prefab.transform.Find("AutoAcceptGroup/PetAuto/Image").GetComponent<Image>();
        var masterAutoAsset = prefab.transform.Find("AutoAcceptGroup/MasterAuto/Image").GetComponent<Image>();

        return (imageAsset.material, textAsset.fontMaterial, petAutoAsset.material, masterAutoAsset.material);
    }
}