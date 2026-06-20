using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    public class CreateConditionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass4048), nameof(GClass4048.Create));
        }

        [PatchPrefix]
        public static bool Prefix(Condition condition, ref ConditionProgressChecker __result)
        {
            if (condition is ConditionSalvage salvage)
            {
                __result = new SalvageConditionChecker(salvage);
                return false;
            }

            if (condition is ConditionLeaveItemAtLocation leaveitem)
            {
                __result = new LeaveItemAtLocationChecker(leaveitem);
                return false;
            }

            return true;
        }
    }
}
