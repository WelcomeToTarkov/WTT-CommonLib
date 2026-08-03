#if !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.InventoryLogic;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Services;

public abstract class CustomTemplateIdToObjectService
{
    /// <summary>
    /// Public API for other mods to register custom item templates.
    /// Reference the CommonLib DLL and call this method with your custom mappings.
    /// </summary>
    public static void AddNewTemplateIdToObjectMapping(List<TemplateIdToObjectType> mappings)
    {
        Type templateIdToObjectMappingsClass = typeof(JsonTypes);

        foreach (var mapping in mappings)
        {
            // Add to TypeTable
            FieldInfo typeTableField = templateIdToObjectMappingsClass.GetField(
                "TypeTable",
                BindingFlags.Public | BindingFlags.Static
            );
            if (typeTableField != null)
            {
                var typeTable = (Dictionary<string, Type>)typeTableField.GetValue(null);
                if (!typeTable.ContainsKey(mapping.TemplateId) && mapping.ItemType != null)
                {
                    typeTable.Add(mapping.TemplateId, mapping.ItemType);
                    LogHelper.LogDebug($"Added {mapping.ItemType.Name} to TypeTable.");
                }
            }

            // Add to TemplateTypeTable
            FieldInfo templateTypeTableField = templateIdToObjectMappingsClass.GetField(
                "TemplateTypeTable",
                BindingFlags.Public | BindingFlags.Static
            );
            if (templateTypeTableField != null)
            {
                var templateTypeTable =
                    (Dictionary<string, Type>)templateTypeTableField.GetValue(null);
                if (!templateTypeTable.ContainsKey(mapping.TemplateId))
                {
                    templateTypeTable.Add(mapping.TemplateId, mapping.TemplateType);
                    LogHelper.LogDebug($"Added {mapping.TemplateType.Name} to TemplateTypeTable.");
                }
            }

            // Add to ItemConstructors only if ItemType is not null
            if (mapping.ItemType != null)
            {
                FieldInfo itemConstructorsField = templateIdToObjectMappingsClass.GetField(
                    "ItemConstructors",
                    BindingFlags.Public | BindingFlags.Static
                );
                if (itemConstructorsField != null)
                {
                    var itemConstructors =
                        (Dictionary<string, Func<string, object, Item>>)
                            itemConstructorsField.GetValue(null);
                    if (!itemConstructors.ContainsKey(mapping.TemplateId))
                    {
                        itemConstructors.Add(mapping.TemplateId, mapping.Constructor);
                        LogHelper.LogDebug(
                            $"Added {mapping.ItemType.Name} constructor to ItemConstructors."
                        );
                    }
                }
            }
        }
    }
}
#endif
