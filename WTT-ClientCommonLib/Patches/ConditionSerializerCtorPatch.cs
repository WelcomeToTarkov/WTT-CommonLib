using Comfort.Common;
using EFT;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class ConditionSerializerCtorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1871).GetConstructor([])!;
        }

        [PatchPostfix]
        public static void Postfix(GClass1871 __instance)
        {
            __instance.List_0.Add(typeof(ConditionSalvage));
        }
    }


}
