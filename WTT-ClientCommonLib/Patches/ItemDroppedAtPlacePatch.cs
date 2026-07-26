using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Helpers;

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
                        !cc.Conditions.Any(cond =>
                            cond is ConditionZone zone
                            && (zone is ConditionSalvage || zone is ConditionLeaveItemAtLocation)
                        )
                    )
                        continue;

                    ConditionProgressChecker ccCpc = null;
                    ConditionSalvage salvageCond = null;
                    ConditionLeaveItemAtLocation leaveCond = null;
                    SalvageConditionChecker salvageCpc = null;
                    LeaveItemAtLocationChecker leaveCpc = null;

                    ccCpc = quest.ProgressCheckers.TryGetValue(cc, out var cpc) ? cpc : null;

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
                            salvageCpc = (SalvageConditionChecker)scpc;
                        }
                        else if (
                            child is ConditionLeaveItemAtLocation
                            && quest.ProgressCheckers.TryGetValue(child, out var lcpc)
                        )
                        {
                            leaveCond = (ConditionLeaveItemAtLocation)child;
                            leaveCpc = (LeaveItemAtLocationChecker)lcpc;
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

                    if (ccCpc != null && !ccCpc.HasGetter())
                    {
                        var target = cc.value;
                        if (target <= 0)
                            target = 1;

                        ccCpc.SetCurrentValueGetter(_ =>
                            (state.SalvageDone && state.LeaveDone) ? target : 0.0
                        );
                    }

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
                            salvageCpc.CurrentValue_1 = true;
                            SignalConditionCompleted(player, quest, status, salvageCond);
                        }

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
                        var completed = TryCompleteCounterCreator(cc, ccCounter, state, ccCpc);
                        if (completed)
                        {
                            SignalConditionCompleted(player, quest, status, cc);
                        }
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

        controller.OnConditionValueChanged(quest, status, condition, true);
    }

    private static bool TryCompleteCounterCreator(
        ConditionCounterCreator cc,
        TaskConditionCounter ccCounter,
        NestedQuestZonesCounterState.State state,
        ConditionProgressChecker ccCpc
    )
    {
        if (!state.SalvageDone || !state.LeaveDone)
            return false;

        if (ccCpc == null)
            return false;

        var target = cc.value;
        if (target <= 0)
            target = 1;

        if (ccCounter != null && ccCounter.Value < target)
        {
            ccCounter.Value = (int)target;
        }

        return ccCpc.Test(target);
    }
}
