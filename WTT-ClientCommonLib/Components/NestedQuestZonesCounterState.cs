using EFT.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WTTClientCommonLib.Components
{
    public static class NestedQuestZonesCounterState
    {
        public class State
        {
            public bool SalvageDone;
            public bool LeaveDone;
        }

        private static readonly Dictionary<(string questId, string ccId), State> _states = new();

        public static State GetOrCreate(QuestClass quest, ConditionCounterCreator cc)
        {
            var key = (quest.Id, cc.id);
            if (!_states.TryGetValue(key, out var state))
            {
                state = new State();
                _states[key] = state;
            }
            return state;
        }

        public static void ResetForQuest(QuestClass quest)
        {
            var keys = _states.Keys.Where(k => k.questId == quest.Id).ToList();
            foreach (var key in keys)
                _states.Remove(key);
        }

        public static void ClearAll()
        {
            _states.Clear();
        }
    }
}
