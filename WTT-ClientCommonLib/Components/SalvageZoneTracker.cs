using System.Collections.Generic;
using EFT;

namespace WTTClientCommonLib.Components
{
    internal static class SalvageZoneTracker
    {
        private static readonly Dictionary<Player, SalvageItemTrigger> _active = new();

        public static void Set(Player player, SalvageItemTrigger zone)
        {
            if (player == null)
                return;
            _active[player] = zone;
        }

        public static void Clear(Player player, SalvageItemTrigger zone)
        {
            if (player == null)
                return;

            if (_active.TryGetValue(player, out var current) && current == zone)
                _active.Remove(player);
        }

        public static SalvageItemTrigger Get(Player player)
        {
            if (player == null)
                return null;
            return _active.TryGetValue(player, out var zone) ? zone : null;
        }
    }
}
