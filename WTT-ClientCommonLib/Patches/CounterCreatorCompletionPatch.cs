using System.Reflection;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class CounterCreatorCompletionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Quest), nameof(Quest.IsConditionDone));
        }

        [PatchPostfix]
        public static void Postfix(Quest __instance, Condition condition, ref bool __result)
        {
            if (__instance == null || condition == null)
                return;

            if (condition is ConditionCounterCreator cc)
            {
                __result = IsCounterCreatorComplete(__instance, cc);
                return;
            }

            if (condition is ConditionSalvage || condition is ConditionLeaveItemAtLocation)
            {
                var parent = FindParentCounterCreator(__instance, condition);
                if (parent != null && IsCounterCreatorComplete(__instance, parent))
                {
                    __result = true;
                }
            }
        }

        private static bool IsCounterCreatorComplete(Quest quest, ConditionCounterCreator cc)
        {
            if (quest == null || cc == null)
                return false;

            if (quest.CompletedConditions != null && quest.CompletedConditions.Contains(cc.id))
                return true;

            var target = cc.value;
            if (target <= 0)
                target = 1;

            var counter = quest.ConditionCountersManager?.GetCounter(cc.id);
            if (counter != null && counter.Value >= target)
                return true;

            if (
                quest.ProgressCheckers != null
                && quest.ProgressCheckers.TryGetValue(cc, out var ccCpc)
                && ccCpc != null
                && ccCpc.CurrentValue >= target
            )
            {
                return true;
            }

            return false;
        }

        private static ConditionCounterCreator? FindParentCounterCreator(
            Quest quest,
            Condition child
        )
        {
            if (quest == null || child == null || child.ParentId == null)
                return null;

            var parentId = child.ParentId.Value;

            if (quest.Conditions == null)
                return null;

            foreach (var kvp in quest.Conditions)
            {
                var conditions = kvp.Value;
                if (conditions == null)
                    continue;

                foreach (var cond in conditions)
                {
                    if (cond is ConditionCounterCreator cc && cc.id == parentId)
                        return cc;
                }
            }

            return null;
        }
    }
}
