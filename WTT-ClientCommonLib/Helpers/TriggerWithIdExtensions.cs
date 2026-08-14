using EFT.Interactive;
using EFT.Quests;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Helpers;

/// <summary>
///     Extensions to the TriggerWithId class
/// </summary>
public static class TriggerWithIdExtensions
{
    /// <summary>
    ///     Attempt to get the time restriction for a specified trigger.
    /// </summary>
    /// <param name="trigger">The key <see cref="TriggerWithId"/> to look up in the trigger/time restriction map</param>
    /// <param name="result">The resulting <see cref="ConditionTimeComparer"/>, or null if one does not exist</param>
    /// <returns>False if <paramref name="result"/> is null, true if not.</returns>
    public static bool TryGetTimeRestrictions(this TriggerWithId trigger, out ConditionTimeComparer? result)
    {
        result = ZoneTimeRestrictionTracker.Get(trigger);
        return result != null;
    }
}