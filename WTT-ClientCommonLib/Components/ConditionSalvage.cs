using EFT.Quests;

namespace WTTClientCommonLib.Components;

public class ConditionSalvage : ConditionZone
{
    public float plantTime;

    public override string FormattedDescription
    {
        get
        {
            return string.Format(base.FormattedDescription, this.zoneId, this.plantTime);
        }
    }
}

