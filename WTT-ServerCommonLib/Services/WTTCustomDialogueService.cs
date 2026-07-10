using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomDialogueService(
    ModHelper modHelper,
    ISptLogger<WTTCustomDialogueService> logger,
    ConfigHelper configHelper,
    TemplateTable templateTable
)
{
    /// <summary>
    /// Loads custom NPC Dialogues from JSON/JSONC files and registers them to the game database.
    ///
    /// Dialogues are loaded from the mod's "config/CustomDialogues" directory (or a custom path if specified).
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomDialogues(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomDialogues");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
        {
            logger.Warning($"Custom dialogues directory not found at {finalDir}");
            return;
        }

        var jsonFiles = Directory
            .GetFiles(finalDir, "*.json")
            .Concat(Directory.GetFiles(finalDir, "*.jsonc"))
            .ToArray();

        if (jsonFiles.Length == 0)
        {
            logger.Warning($"No custom dialogue files found in {finalDir}");
            return;
        }

        var traderDialogElements = templateTable.Dialogue.Elements;
        int added = 0;
        foreach (var file in jsonFiles)
        {
            try
            {
                var dialogueData = await configHelper.LoadJsonFile<List<TraderDialogElement>>(file);

                if (dialogueData == null)
                {
                    logger.Error($"Failed to load dialogue data from {file}");
                    continue;
                }

                if (dialogueData.Count == 0)
                {
                    logger.Warning($"Dialogue file {Path.GetFileName(file)} is empty");
                    continue;
                }

                foreach (var element in dialogueData)
                {
                    if (traderDialogElements.All(d => d.Id != element.Id))
                    {
                        traderDialogElements.Add(element);
                        added++;
                    }
                    else
                    {
                        logger.Warning(
                            $"Dialogue element with ID {element.Id} already exists, skipping"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading dialogue file {file}: {ex.Message}");
            }
        }

        if (added > 0)
        {
            LogHelper.Debug(logger, $"Successfully added {added} custom dialogue to the server!");
        }
    }
}
