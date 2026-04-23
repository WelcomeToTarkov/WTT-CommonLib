using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using WTTClientCommonLib.Attributes;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Services;

public class CustomParentLoader
{
    public static CustomParentLoader Instance;
    
    private Dictionary<string, ItemTemplate> _customParents = new Dictionary<string, ItemTemplate>();
    private bool _parentsRegistered = false;

    public CustomParentLoader()
    {
        if (Instance != null) return;
        
        Instance = this;
    }
    
    public void FetchCustomParentsFromServer()
    {
        try
        {
            var parentData = Utils.Get<Dictionary<string, ItemTemplate>>("/wttcommonlib/parents/custom/get");

            if (parentData != null)
            {
                _customParents = parentData;
            }
            else
            {
                LogHelper.LogError("Failed to fetch custom parent data from server");
            }
        }
        catch (Exception e)
        {
            LogHelper.LogError(e.ToString());
            throw;
        }
    }

    public async Task RegisterCustomParents()
    {
        if (_parentsRegistered) return;

        // wait for other plugins to load
        await Task.Delay(5000);
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<CustomParent> customParents = [];
        List<TemplateIdToObjectType> mappings = [];
        
        // load all classes that use customparent attribute
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(CustomParent), true);

                foreach (CustomParent attribute in attributes)
                {
                    customParents.Add(attribute);
                }
            }
        }
        
        foreach (var parent in customParents)
        {
            var ctor = parent.Item.GetConstructor([typeof(string), parent.Template]);

            if (ctor == null)
            {
                LogHelper.LogError($"Could not find valid constructor for {parent.Item}");
                continue;
            }
            
            mappings.Add(new TemplateIdToObjectType(
                parent.ParentId,
                parent.Item,
                parent.Template,
                (id, tpl) => (Item)ctor.Invoke([id, tpl])
            ));
        }
        
        CustomTemplateIdToObjectService.AddNewTemplateIdToObjectMapping(mappings);
        
        _parentsRegistered = true;
    }
}
