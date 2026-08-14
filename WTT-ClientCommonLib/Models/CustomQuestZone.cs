using System.Collections.Generic;

namespace WTTClientCommonLib.Models;


public class CustomQuestZone
{
    public string ZoneId { get; set; }
    public string ZoneName { get; set; }
    public string ZoneLocation { get; set; }
    public string ZoneType { get; set; }
    public string FlareType { get; set; }

    public ZoneTransform Position { get; set; }
    public ZoneTransform Rotation { get; set; }
    public ZoneTransform Scale { get; set; }

    public List<ZoneTransforms>? GroupPosition { get; set; }

    public SalvageConfig? Salvage { get; set; }
}

public class SalvageConfig
{
    public string RequiredItemTpl { get; set; } = "";
    public float SalvageTime { get; set; } = 10f;
    public bool ConsumeRequiredItem { get; set; } = true;

    public List<SalvageRewardConfig> Rewards { get; set; } = new();
}

public class SalvageRewardConfig
{
    public string ItemTpl { get; set; } = "";
    public int Count { get; set; } = 1;
    public bool ToQuestInventory { get; set; } = false;
}