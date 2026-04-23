using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using System.Reflection;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomParentService(
    ISptLogger<WTTCustomParentService> logger,
    DatabaseService dbService,
    ItemBaseClassService baseClassService,
    ModHelper modHelper,
    ConfigHelper configHelper,
    ItemBaseClassService itemBaseClassService
)
{
    private readonly Dictionary<MongoId, TemplateItem> _loadedParents = [];
    
    /// <summary>
    /// Loads custom parent configs from json and jsonc files and saves them in the spt database.
    /// Parents are loaded from "db/CustomParents" directory by default (or a custom path if specified).
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomParents(Assembly assembly, string relativePath = "db/CustomParents")
    {
        string modDir = modHelper.GetAbsolutePathToModFolder(assembly);
        string parentDir = Path.Combine(modDir, relativePath);

        if (!Directory.Exists(parentDir))
        {
            logger.Error($"Could not find parent directory {relativePath}");
            return;
        }

        string[] files = Directory.GetFiles(parentDir, "*.json*", SearchOption.AllDirectories);

        foreach (string filePath in files)
        {
            var allParents = await configHelper.LoadJsonFileFlexible<Dictionary<MongoId, TemplateItem>>(filePath);

            if (allParents.Count == 0)
            {
                logger.Warning("No custom parents found");
                return;
            }
            
            foreach (Dictionary<MongoId, TemplateItem> parents in allParents)
            {
                foreach ((MongoId id, TemplateItem tpl) in parents)
                {
                    bool added = AddParentToDatabase(id, tpl);

                    if (added)
                    {
                        _loadedParents[id] = tpl;
                    }
                }
            }
        }
    }

    protected bool AddParentToDatabase(MongoId id, TemplateItem tpl)
    {
        try
        {
            var items = dbService.GetTables().Templates.Items;

            items[id] = tpl;
            itemBaseClassService.AddItemToCache(id);

            LogHelper.Debug(logger, $"Added parent {tpl.Id} to database and cache");
            
            return true;
        }
        catch (Exception e)
        {
            logger.Error(e.Message);
            
            return false;
        }
    }

    public Dictionary<MongoId, TemplateItem> GetCustomParents()
    {
        return _loadedParents;
    }
}
