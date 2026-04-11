using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib.Patches;

public class ProduceViewShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ProduceView), nameof(ProduceView.Show), [
            typeof(ItemUiContext),
            typeof(InventoryController),
            typeof(GClass2440),
            typeof(GClass2431),
            typeof(Action<string>),
            typeof(Action<string>),
            typeof(bool)
        ]);
    }

    [PatchPostfix]
    private static void PatchPostfix(ProduceView __instance, GClass2440 scheme, HideoutItemViewFactory ____resultItemIconViewFactory)
    {
        HideoutItemViewFactory viewFactory = ____resultItemIconViewFactory;
        ExtendedRecipeLoader recipeLoader = ExtendedRecipeLoader.Instance;
        string schemeId = scheme._id;
        ExtendedProductionScheme extendedScheme = recipeLoader.GetExtendedScheme(schemeId);

        if (extendedScheme != null)
        {
            LogHelper.LogInfo($"found extended scheme for craft scheme: {schemeId}");
            
            // TODO: add support for multiple recipe results
            Item recipeResult = extendedScheme.BaseItems[0];
            viewFactory.Show(recipeResult, __instance.InventoryController, __instance.ItemUiContext);

            if (extendedScheme.count > 1)
            {
                viewFactory.SetCounterText(extendedScheme.count.ToString());
                viewFactory.ShowInfo(true, false);
            }
            else
            {
                viewFactory.ShowInfo(false, false);
            }
        }
    }
}
