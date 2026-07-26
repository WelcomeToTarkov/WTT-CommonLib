using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class Salvage_AddTriggerZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.AddTriggerZone));
        }

        [PatchPostfix]
        public static void Postfix(Player __instance, TriggerWithId zone)
        {
            if (zone is not SalvageItemTrigger salvage)
                return;

            SalvageZoneTracker.Set(__instance, salvage);

            __instance.SearchForInteractions();
        }
    }

    internal class Salvage_RemoveTriggerZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.RemoveTriggerZone));
        }

        [PatchPostfix]
        public static void Postfix(Player __instance, TriggerWithId zone)
        {
            if (zone is not SalvageItemTrigger salvage)
                return;

            SalvageZoneTracker.Clear(__instance, salvage);

            __instance.SearchForInteractions();
        }
    }

    internal class Salvage_InteractionsChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(GamePlayerOwner),
                nameof(GamePlayerOwner.InteractionsChangedHandler)
            );
        }

        [PatchPrefix]
        private static bool Prefix(GamePlayerOwner __instance)
        {
            var player = __instance.Player;
            if (player == null)
                return true;

            if (
                player.InteractableObject != null
                || player.PlaceItemZone != null
                || player.BtrInteractionSide != null
                || player.TripwireInteractionTrigger != null
                || player.EventObjectInteractive != null
                || player.ExfiltrationPoint != null
            )
            {
                return true;
            }

            var salvage = SalvageZoneTracker.Get(player);
            if (salvage == null)
                return true;

            IInteractive interactive = salvage;

            var actions = InteractionContextHelper.GetAvailableActions(__instance, interactive);
            ClientTransitController transit;
            if (actions == null && TransitController.Exist(out transit))
            {
                actions = transit._availableInteractionState;
            }

            if (actions != null)
                actions.InitSelected();

            __instance.AvailableInteractionState.Value = actions;
            return false;
        }
    }
}
