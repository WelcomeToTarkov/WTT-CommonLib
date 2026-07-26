using System.Reflection;
using EFT.Quests;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Patches
{
    internal class ConditionSerializerCtorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ConditionSerializer).GetConstructor([])!;
        }

        [PatchPostfix]
        public static void Postfix(ConditionSerializer __instance)
        {
            __instance._types.Add(typeof(ConditionSalvage));
        }
    }
}
