using System;

namespace WTTClientCommonLib.Attributes;

/// <summary>
/// Declares a mapping between a parent template ID and the corresponding item and template types
/// </summary>
/// <param name="id">MongoID of the parent item, has to match the id of a valid parent on the server</param>
/// <param name="item">The item class that should be associated with this parent (CAN BE NULL FOR TEMPLATE REGISTRATION)</param>
/// <param name="template">The data template class that should be associated with this item</param>
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
