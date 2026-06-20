using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class LocalRaidEndedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(Class308),
                "LocalRaidEnded",
                new[]
                {
                    typeof(LocalRaidSettings),
                    typeof(RaidEndDescriptorClass),
                    typeof(FlatItemsDataClass[]),
                    typeof(Dictionary<string, FlatItemsDataClass[]>)
                });
        }

        [PatchPostfix]
        public static void Postfix(
            LocalRaidSettings settings,
            RaidEndDescriptorClass results,
            FlatItemsDataClass[] lostInsuredItems,
            Dictionary<string, FlatItemsDataClass[]> transferItems)
        {
            var world = Singleton<GameWorld>.Instance;
            var player = world?.MainPlayer;
            var questController = player?.AbstractQuestControllerClass;
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
