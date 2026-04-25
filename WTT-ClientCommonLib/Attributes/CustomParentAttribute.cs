using EFT.InventoryLogic;
using System;

namespace WTTClientCommonLib.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CustomParent : Attribute
{
    public string ParentId { get; }
    public Type? Item { get; }
    public Type? Template { get; }

    public CustomParent(string id, Type? item, Type? template)
    {
        ParentId = id;
        Item = item;
        Template = template;
    }
}
