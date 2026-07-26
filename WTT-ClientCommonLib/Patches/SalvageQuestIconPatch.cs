using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib.Patches
{
    internal class SalvageQuestIconPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StaticIcons), "GetQuestIcon");
        }

        [PatchPostfix]
        public static void Postfix(StaticIcons __instance, Condition condition, ref Sprite __result)
        {
            if (__result != null || condition == null)
                return;

            if (condition is ConditionSalvage)
            {
                var sprite = QuestIcons.SalvageSprite;
                if (sprite != null)
                {
                    __result = sprite;
                }
            }
        }
    }
}