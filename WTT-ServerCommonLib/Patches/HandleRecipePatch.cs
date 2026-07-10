using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using WTTServerCommonLib.Controllers;
using WTTServerCommonLib.Services;

namespace WTTServerCommonLib.Patches;

[Injectable]
public class HandleRecipePatch(
    WTTHideoutControllerExtended wttHideoutControllerExtended,
    WTTCustomHideoutRecipeService wttCustomHideoutRecipeService
) : AbstractPatch
{
    private static WTTHideoutControllerExtended? _hideoutControllerExtended;
    private static WTTCustomHideoutRecipeService? _customHideoutRecipeService;

    protected override MethodBase? GetTargetMethod()
    {
        _hideoutControllerExtended = wttHideoutControllerExtended;
        _customHideoutRecipeService = wttCustomHideoutRecipeService;

        return AccessTools.Method(typeof(HideoutController), "HandleRecipe");
    }

    [PatchPrefix]
    private static bool PatchPrefix(
        MongoId sessionID,
        HideoutProduction recipe,
        PmcData pmcData,
        HideoutTakeProductionRequestData request,
        ItemEventRouterResponse output
    )
    {
        if (_hideoutControllerExtended == null || _customHideoutRecipeService == null)
            return true;

        var extendedRecipe = _customHideoutRecipeService.GetExtendedRecipe(recipe.Id);

        if (extendedRecipe != null)
        {
            _hideoutControllerExtended.HandleExtendedRecipe(
                sessionID,
                recipe,
                pmcData,
                request,
                output
            );
            return false;
        }

        return true;
    }
}
