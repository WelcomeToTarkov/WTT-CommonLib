using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using System.Reflection;
using WTTServerCommonLib.Controllers;

namespace WTTServerCommonLib.Patches;

public class HandleRecipePatch : AbstractPatch
{
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutController), "HandleRecipe");
    }

    [PatchPrefix]
    private static bool PatchPrefix(
        MongoId sessionID,
        HideoutProduction recipe,
        PmcData pmcData,
        HideoutTakeProductionRequestData request,
        ItemEventRouterResponse output)
    {
        var hideoutControllerExtended = ServiceLocator.ServiceProvider.GetService<WTTHideoutControllerExtended>();

        if (hideoutControllerExtended != null)
        {
            hideoutControllerExtended.HandleExtendedRecipe(sessionID, recipe, pmcData, request, output);
            return false;
        }
        
        return true;
    }
}
