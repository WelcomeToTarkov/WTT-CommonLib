using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;

internal class ItemDroppedAtPlacePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Profile), nameof(Profile.ItemDroppedAtPlace));
    }

    [PatchPrefix]
    public static bool Prefix(Profile __instance, string itemId, string zoneId)
    {
        var world = Singleton<GameWorld>.Instance;
        var player = world?.MainPlayer;
        var questController = player.QuestController;
        var questBook = questController?.Quests;

        if (questBook == null)
            return true;

        foreach (var quest in questBook)
        {
            if (quest.ProgressCheckers == null || quest.Conditions == null)
                continue;

            foreach (var kvp in quest.Conditions)
            {
                var status = kvp.Key;
                var bucket = kvp.Value;
                if (bucket == null)
                    continue;

                foreach (var root in bucket)
                {
                    if (root is not ConditionCounterCreator cc || cc.Conditions == null)
                        continue;

                    if (
                        !cc.Conditions.Any(c =>
                            c is ConditionZone z
                            && (z is ConditionSalvage || z is ConditionLeaveItemAtLocation)
                        )
                    )
                        continue;

                    ConditionSalvage salvageCond = null;
                    ConditionLeaveItemAtLocation leaveCond = null;
                    ConditionProgressChecker salvageCpc = null;
                    ConditionProgressChecker leaveCpc = null;

                    foreach (var child in cc.Conditions)
                    {
                        if (child is not ConditionZone zoneChild)
                            continue;

                        if (zoneChild.zoneId != zoneId)
                            continue;

                        if (
                            child is ConditionSalvage
                            && quest.ProgressCheckers.TryGetValue(child, out var scpc)
                        )
                        {
                            salvageCond = (ConditionSalvage)child;
                            salvageCpc = scpc;
                        }
                        else if (
                            child is ConditionLeaveItemAtLocation
                            && quest.ProgressCheckers.TryGetValue(child, out var lcpc)
                        )
                        {
                            leaveCond = (ConditionLeaveItemAtLocation)child;
                            leaveCpc = lcpc;
                        }
                    }

                    if (salvageCond == null && leaveCond == null)
                        continue;

                    var countersManager = quest.ConditionCountersManager;
                    TaskConditionCounter ccCounter = null;
                    if (countersManager != null)
                    {
                        ccCounter = countersManager.GetCounter(cc.id);
                    }

                    var state = NestedQuestZonesCounterState.GetOrCreate(quest, cc);

                    if (salvageCond != null && salvageCpc != null)
                    {
                        if (state.SalvageDone)
                        {
                            return false;
                        }

                        var needed = salvageCond.value;
                        var salvageDone = salvageCpc.Test(needed);

                        if (salvageDone)
                        {
                            state.SalvageDone = true;
                            SignalConditionCompleted(player, quest, status, salvageCond);
                        }

                        TryCompleteCounterCreator(cc, ccCounter, state);
                        return false;
                    }

                    // LeaveItem branch
                    if (leaveCond != null && leaveCpc != null)
                    {
                        var neededLeave = leaveCond.value;
                        var leaveDone = leaveCpc.Test(neededLeave);

                        if (leaveDone)
                        {
                            state.LeaveDone = true;
                            SignalConditionCompleted(player, quest, status, leaveCond);
                        }

                        TryCompleteCounterCreator(cc, ccCounter, state);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static void SignalConditionCompleted(
        Player player,
        Quest quest,
        EQuestStatus status,
        Condition condition
    )
    {
        var controller = player?.QuestController;
        if (controller == null)
            return;

        var type = controller.GetType();
        var onCondField = type.GetField(
            "OnConditionValueChanged",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        var del = onCondField?.GetValue(controller) as Action<Quest, EQuestStatus, Condition, bool>;
        del?.Invoke(quest, status, condition, true);
    }

    private static void TryCompleteCounterCreator(
        ConditionCounterCreator cc,
        TaskConditionCounter ccCounter,
        NestedQuestZonesCounterState.State state
    )
    {
        if (!state.SalvageDone || !state.LeaveDone)
            return;

        if (ccCounter == null)
            return;

        var target = cc.value;
        if (target <= 0)
            target = 1;

        if (ccCounter.Value >= target)
            return;

        ccCounter.Value = (int)target;
    }
}
