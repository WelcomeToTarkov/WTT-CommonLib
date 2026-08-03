using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class GetConditionHandlersByZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type[] parameters = [typeof(string)];
            Type[] generics = [typeof(ConditionZone)];
            return AccessTools.Method(
                typeof(QuestBook),
                nameof(QuestBook.GetConditionHandlersByZone),
                parameters,
                generics
            );
        }

        [PatchPostfix]
        public static void Postfix(
            QuestBook __instance,
            string zoneId,
            ref IEnumerable<ConditionProgressChecker> __result
        )
        {
            var list = __result?.ToList() ?? new List<ConditionProgressChecker>();

            foreach (var quest in __instance)
            {
                if (quest == null)
                    continue;

                if (
                    quest.QuestStatus != EQuestStatus.Started
                    && quest.QuestStatus != EQuestStatus.AvailableForFinish
                )
                    continue;

                if (quest.Conditions == null || quest.ProgressCheckers == null)
                    continue;

                foreach (var kvp in quest.Conditions)
                {
                    var status = kvp.Key;
                    var conditions = kvp.Value;

                    if (conditions == null)
                        continue;

                    if (
                        !quest.CurrentStatusTransitions.Contains(status)
                        && status != quest.QuestStatus
                    )
                        continue;

                    foreach (var condition in conditions)
                    {
                        if (condition is not ConditionCounterCreator cc || cc.Conditions == null)
                            continue;

                        if (IsCounterCreatorComplete(quest, cc))
                            continue;

                        foreach (var child in cc.Conditions)
                        {
                            if (child is not ConditionZone zoneChild)
                                continue;

                            if (zoneChild.zoneId != zoneId)
                                continue;

                            if (
                                zoneChild is not ConditionLeaveItemAtLocation
                                && zoneChild is not ConditionSalvage
                            )
                                continue;

                            if (
                                !quest.ProgressCheckers.TryGetValue(child, out var cpc)
                                || cpc == null
                            )
                                continue;

                            if (!list.Contains(cpc))
                            {
                                list.Add(cpc);
                            }
                        }
                    }
                }
            }

            __result = list;
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
    }
}
