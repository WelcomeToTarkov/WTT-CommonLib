using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace WTTServerCommonLib.Helpers;

[Injectable]
public class QuestHelper(ISptLogger<QuestHelper> logger)
{
    public void AddDogtagsToQuests(
        Dictionary<MongoId, Quest> quests,
        string questId,
        List<MongoId> dogtagIds,
        string faction
    )
    {
        if (!quests.TryGetValue(questId, out var quest))
        {
            logger.Warning($"Quest {questId} not found");
            return;
        }

        if (quest.Conditions.AvailableForFinish == null)
        {
            logger.Warning($"Quest {questId} has no AvailableForFinish conditions");
            return;
        }

        var factionId = faction.ToUpper() switch
        {
            "USEC" => "59f32c3b86f77472a31742f0",
            "BEAR" => "59f32bb586f774757e1e8442",
            _ => throw new ArgumentException($"Invalid faction: {faction}. Use 'USEC' or 'BEAR'"),
        };

        var modified = false;

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            if (
                condition is { ConditionType: "HandoverItem" }
                && condition.Target?.List != null
                && condition.Target.List.Contains(factionId)
            )
            {
                foreach (var dogtagId in dogtagIds)
                {
                    if (!condition.Target.List.Contains(dogtagId))
                    {
                        condition.Target.List.Add(dogtagId);
                        modified = true;
                        logger.Debug($"Added {faction} dogtag {dogtagId} to quest {questId}");
                    }
                }
            }
        }

        if (modified)
        {
            logger.Debug($"Successfully added {faction} dogtags to quest {questId}");
        }
        else
        {
            logger.Warning($"No {faction} dogtag HandoverItem condition found in quest {questId}");
        }
    }

    public void AddWeaponsToKillCondition(
        Dictionary<MongoId, Quest> quests,
        string questId,
        string[] weaponIds
    )
    {
        if (!quests.TryGetValue(questId, out var quest))
        {
            logger.Warning($"Quest {questId} not found");
            return;
        }

        if (quest.Conditions.AvailableForFinish == null)
        {
            logger.Warning($"Quest {questId} has no AvailableForFinish conditions");
            return;
        }

        var modified = false;

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            logger.Debug($"Checking condition type: {condition.ConditionType}");

            if (condition is { ConditionType: "CounterCreator", Counter.Conditions: not null })
            {
                foreach (var counterCond in condition.Counter.Conditions)
                {
                    logger.Debug($"  Counter condition type: {counterCond.ConditionType}");

                    if (counterCond is { Weapon: not null, ConditionType: "Kills" or "Shots" })
                    {
                        var beforeCount = counterCond.Weapon.Count;

                        if (beforeCount == 0)
                        {
                            logger.Debug("  Skipping empty weapon array");
                            continue;
                        }

                        foreach (var weaponId in weaponIds)
                        {
                            if (counterCond.Weapon.Add(weaponId))
                            {
                                modified = true;
                                logger.Debug($"    Added weapon {weaponId}");
                            }
                        }

                        logger.Debug(
                            $"  Weapon count before: {beforeCount}, after: {counterCond.Weapon.Count}"
                        );
                    }
                }
            }
        }

        if (modified)
        {
            logger.Debug($"Successfully modified quest {questId}");
        }
        else
        {
            logger.Warning(
                $"No modifications made to quest {questId} - condition structure might differ"
            );
        }
    }

    public void AddArmorToEquipmentExclusive(
        Dictionary<MongoId, Quest> quests,
        string questId,
        string[] armorIds
    )
    {
        if (
            !quests.TryGetValue(questId, out var quest)
            || quest.Conditions.AvailableForFinish == null
        )
            return;

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            if (condition is { ConditionType: "CounterCreator", Counter.Conditions: not null })
            {
                foreach (var counterCond in condition.Counter.Conditions)
                {
                    if (counterCond is { ConditionType: "Equipment", EquipmentExclusive: not null })
                    {
                        foreach (var armorId in armorIds)
                        {
                            counterCond.EquipmentExclusive.Add([armorId]);
                        }
                    }
                }
            }
        }
    }

    public void AddWeaponsToFindOrHandoverCondition(
        Dictionary<MongoId, Quest> quests,
        string questId,
        string[] weaponIds
    )
    {
        if (
            !quests.TryGetValue(questId, out var quest)
            || quest.Conditions.AvailableForFinish == null
        )
            return;

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            if (
                (condition.ConditionType == "FindItem" || condition.ConditionType == "HandoverItem")
                && condition.Target != null
            )
            {
                foreach (var weaponId in weaponIds)
                {
                    if (condition.Target.List != null && !condition.Target.List.Contains(weaponId))
                    {
                        condition.Target.List.Add(weaponId);
                    }
                }
            }
        }
    }

    public void AddWeaponModToCondition(
        Dictionary<MongoId, Quest> quests,
        string questId,
        string modId,
        string existingModId,
        bool isInclusive = true
    )
    {
        if (
            !quests.TryGetValue(questId, out var quest)
            || quest.Conditions.AvailableForFinish == null
        )
        {
            logger.Warning($"Quest {questId} not found or has no AvailableForFinish conditions");
            return;
        }

        var modType = isInclusive ? "Inclusive" : "Exclusive";
        var modified = false;

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            if (condition is { ConditionType: "CounterCreator", Counter.Conditions: not null })
            {
                foreach (var counterCond in condition.Counter.Conditions)
                {
                    var modList = isInclusive
                        ? (List<List<string>>)counterCond.WeaponModsInclusive
                        : (List<List<string>>)counterCond.WeaponModsExclusive;

                    if (modList != null)
                    {
                        // Check if existing mod exists anywhere in the list of arrays
                        var existingModExists = modList.Any(list => list.Contains(existingModId));

                        if (existingModExists)
                        {
                            // Check if our mod already exists
                            var modAlreadyExists = modList.Any(list => list.Contains(modId));

                            if (!modAlreadyExists)
                            {
                                // Add as a new single-item array
                                modList.Add([modId]);
                                modified = true;
                                logger.Debug(
                                    $"Added mod {modId} to WeaponMods{modType} in quest {questId}"
                                );
                            }
                        }
                    }
                }
            }
        }

        if (modified)
        {
            logger.Debug($"Successfully added mod to quest {questId}");
        }
        else
        {
            logger.Warning(
                $"Existing mod {existingModId} not found in WeaponMods{modType} for quest {questId}"
            );
        }
    }
}
