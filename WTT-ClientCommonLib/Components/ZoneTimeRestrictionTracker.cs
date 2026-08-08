using System.Collections.Generic;
using EFT.Interactive;
using EFT.Quests;

namespace WTTClientCommonLib.Components;

/// <summary>
///     Track which active zones have their interactions locked behind time-of-day restrictions
/// </summary>
public static class ZoneTimeRestrictionTracker
{
    /// <summary>
    ///     A map of individual triggers (ie SalvageItemTrigger or PlaceItemTrigger) and their time restrictions.
    ///     It uses ConditionTimeComparer, a vanilla class only used in ConditionHit.
    /// </summary>
    private static readonly Dictionary<TriggerWithId, ConditionTimeComparer> Active = new();
    
    /// <summary>
    ///     Register a trigger as time-restricted. Overwrites any existing restriction already tracked for the trigger.
    /// </summary>
    /// <param name="trigger">The trigger the restriction applies to</param>
    /// <param name="timeRestriction">The time window the trigger's interaction is restricted to</param>
    public static void Set(TriggerWithId trigger, ConditionTimeComparer timeRestriction)
    {
        if (!trigger) { return; }
        
        Active[trigger] = timeRestriction;
    }

    /// <summary>
    ///     Remove a trigger's time restriction, if one is tracked.
    /// </summary>
    /// <param name="trigger">The trigger to remove the restriction from</param>
    public static void Clear(TriggerWithId trigger)
    {
        if (!trigger) { return; }

        Active.Remove(trigger, out _);
    }

    /// <summary>
    ///     Get the time restriction for a trigger from the map.
    /// </summary>
    /// <param name="trigger">The trigger to get the restriction from</param>
    /// <returns>The <see cref="ConditionTimeComparer"/> restriction, or null if the trigger has no time restriction</returns>
    public static ConditionTimeComparer? Get(TriggerWithId trigger)
    {
        return !trigger ? null : Active.GetValueOrDefault(trigger);
    }
}