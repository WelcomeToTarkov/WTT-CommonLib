using EFT;
using EFT.Interactive;
using EFT.UI;
using UnityEngine;

namespace WTTClientCommonLib.Components;

public class ZoneFlareTrigger : TriggerWithId
{
    public int Experience;

    public void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Triggers");
    }

    public override void TriggerEnter(Player player)
    {
        base.TriggerEnter(player);
#if DEBUG
        ConsoleScreen.Log("WTT-ClientCommonLib: Entered Flare CustomQuestZone.");
#endif
    }
}