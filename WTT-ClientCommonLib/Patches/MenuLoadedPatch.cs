using System.Reflection;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib.Patches;

public class MenuLoadedPatch : ModulePatch
{
    private static bool _menuShown = false;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(MenuScreen),
            nameof(MenuScreen.Show),
            [typeof(Profile), typeof(MatchmakerPlayersController), typeof(ESessionMode)]
        );
    }

    [PatchPostfix]
    private static void PatchPostfix()
    {
        if (!_menuShown)
        {
            ExtendedRecipeLoader.Instance.LoadExtendedRecipeResults();

            _menuShown = true;
        }
    }
}
