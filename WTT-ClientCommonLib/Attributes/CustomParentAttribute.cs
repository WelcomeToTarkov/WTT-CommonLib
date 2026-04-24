using EFT.InventoryLogic;
using System;

namespace WTTClientCommonLib.Attributes;

/// <summary>
/// Declares a mapping between a parent template ID and the corresponding item and template types
/// </summary>
/// <param name="id">MongoID of the parent item, has to match the id of a valid parent on the server</param>
/// <param name="item">The item class that should be associated with this parent</param>
/// <param name="template">The data template class that should be associated with this item</param>
[AttributeUsage(AttributeTargets.Class)]
public class CustomParent(string id, Type item, Type template) : Attribute
{
    public string ParentId { get; } = id;
    public Type Item { get; } = item;
    public Type Template { get; } = template;
}
