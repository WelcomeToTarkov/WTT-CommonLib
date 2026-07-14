using EFT.Quests;
using System;
using System.Collections.Generic;
using System.Text;

namespace WTTClientCommonLib.Components
{

    public class SalvageConditionChecker : ConditionProgressChecker
    {
        private bool _done;
        private readonly double _target;

        public SalvageConditionChecker(ConditionSalvage condition)
            : base(condition)
        {
            _target = condition.value;

            SetCurrentValueGetter(cpc =>
            {
                var sc = (SalvageConditionChecker)cpc;
                return sc._done ? sc._target : 0.0;
            });
        }

        public override bool Test(object testValue)
        {
            var result = base.Test(testValue);

            if (result && !_done)
            {
                _done = true;
            }

            return result;
        }
    }

    public class LeaveItemAtLocationChecker : ConditionProgressChecker
    {
        private bool _done;
        private readonly double _target;

        public LeaveItemAtLocationChecker(ConditionLeaveItemAtLocation condition)
            : base(condition)
        {
            _target = condition.value;

            SetCurrentValueGetter(cpc =>
            {
                var lic = (LeaveItemAtLocationChecker)cpc;
                return lic._done ? lic._target : 0.0;
            });
        }

        public override bool Test(object testValue)
        {
            var result = base.Test(testValue);

            if (result && !_done)
            {
                _done = true;
            }

            return result;
        }
    }
}
