using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class CounterCreatorChildConditionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ConditionCounterCreator), "OnDeserializedMethod");
        }

        [PatchPostfix]
        public static void Postfix(ConditionCounterCreator __instance, StreamingContext context)
        {
            var nested = __instance.Conditions;
            if (nested == null || nested.Count == 0)
                return;

            var childList = __instance.ChildConditions;
            if (childList == null)
                return;

            foreach (var cond in nested)
            {
                if (cond == null)
                    continue;

                if (cond is not ConditionSalvage &&
                    cond is not ConditionLeaveItemAtLocation)
                    continue;

                if (!childList.Contains(cond))
                {
                    childList.Add(cond);
                }
            }
        }
    }
}