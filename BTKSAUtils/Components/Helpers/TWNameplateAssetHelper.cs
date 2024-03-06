using System.Net.Mime;
using TMPro;
using TotallyWholesome;
using UnityEngine;
using UnityEngine.UI;

namespace BTKSAUtils.Components.Helpers;

public class TWNameplateAssetHelper
{
    public static (Material, Material, Material, Material) GetTWMaterials()
    {
        var imageAsset = TWAssets.StatusPrefab.transform.Find("SpecialMark").GetComponent<Image>();
        var textAsset = imageAsset.transform.Find("SpecialMarkText").GetComponent<TextMeshProUGUI>();
        var petAutoAsset = TWAssets.StatusPrefab.transform.Find("AutoAcceptGroup/PetAuto/Image").GetComponent<Image>();
        var masterAutoAsset = TWAssets.StatusPrefab.transform.Find("AutoAcceptGroup/MasterAuto/Image").GetComponent<Image>();

        return (imageAsset.material, textAsset.fontMaterial, petAutoAsset.material, masterAutoAsset.material);
    }
}