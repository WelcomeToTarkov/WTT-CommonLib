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
                if (
                    quest.QuestStatus != EQuestStatus.Started
                    && quest.QuestStatus != EQuestStatus.AvailableForFinish
                )
                    continue;

                if (quest.Conditions == null)
                    continue;

                foreach (var kvp in quest.Conditions)
                {
                    var status = kvp.Key;
                    var conditions = kvp.Value;

                    if (
                        !quest.CurrentStatusTransitions.Contains(status)
                        && status != quest.QuestStatus
                    )
                        continue;

                    foreach (var cond in conditions)
                    {
                        if (cond is not ConditionCounterCreator cc || cc.Conditions == null)
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
    }
}
