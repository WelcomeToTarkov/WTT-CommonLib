using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using JsonType;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class LocalRaidEndedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(EftClientBackendSession),
                "LocalRaidEnded",
                new[]
                {
                    typeof(LocalRaidSettings),
                    typeof(SessionResult),
                    typeof(FlatItem[]),
                    typeof(Dictionary<string, FlatItem[]>),
                }
            );
        }

        [PatchPostfix]
        public static void Postfix(
            LocalRaidSettings settings,
            SessionResult results,
            FlatItem[] lostInsuredItems,
            Dictionary<string, FlatItem[]> transferItems
        )
        {
            var world = Singleton<GameWorld>.Instance;
            var player = world?.MainPlayer;
            var questController = player?.QuestController;
            var questBook = questController?.Quests;

            if (questBook == null)
            {
                NestedQuestZonesCounterState.ClearAll();
                return;
            }

            foreach (var quest in questBook)
            {
                NestedQuestZonesCounterState.ResetForQuest(quest);
            }
        }
    }
}
