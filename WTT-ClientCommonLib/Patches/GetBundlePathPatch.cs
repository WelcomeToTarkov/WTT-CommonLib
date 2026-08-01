using HarmonyLib;
using SPT.Common.Http;
using SPT.Custom.Models;
using SPT.Custom.Utils;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace WTTClientCommonLib.Patches;

internal class GetBundlePathPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BundleManager), nameof(BundleManager.GetBundlePath));
    }

    [PatchPrefix]
    private static bool Prefix(BundleItem bundle, ref string __result)
    {
        try
        {
            if (!RequestHandler.IsLocal)
            {
                __result = "SPT_Runtime/user/cache/bundles/";
                return false;
            }

            __result = "SPT_Runtime/" + bundle.ModPath + "/bundles/";
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}