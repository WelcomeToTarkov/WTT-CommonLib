using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomHideoutRecipeService(
    ISptLogger<WTTCustomHideoutRecipeService> logger,
    HideoutTable hideoutTable,
    ModHelper modHelper,
    ConfigHelper configHelper
)
{
    private Dictionary<MongoId, HideoutProductionExtended> _extendedRecipes = [];

    public async Task CreateHideoutRecipes(Assembly assembly, string? relativePath = null)
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomHideoutRecipes");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Error($"Directory not found at {finalDir}");
                return;
            }

            var jsonFiles = Directory
                .GetFiles(finalDir, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                    f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();

            var allRecipes = new List<HideoutProductionExtended>();

            foreach (var filePath in jsonFiles)
            {
                var recipes = await configHelper.LoadJsonFileFlexible<HideoutProductionExtended>(
                    filePath
                );

                if (recipes.Count > 0)
                {
                    allRecipes.AddRange(recipes);
                    LogHelper.Debug(
                        logger,
                        $"Loaded {recipes.Count} recipes from {Path.GetFileName(filePath)}"
                    );
                }
                else
                {
                    logger.Warning($"Could not parse recipes from {Path.GetFileName(filePath)}");
                }
            }

            if (allRecipes.Count == 0)
            {
                logger.Warning($"No valid hideout recipes found in {finalDir}");
                return;
            }

            foreach (var recipe in allRecipes)
            {
                if (!MongoId.IsValidMongoId(recipe.Id))
                {
                    logger.Error(
                        $"Missing or invalid Id in recipe for end product {recipe.EndProduct}"
                    );
                    continue;
                }

                var recipeExists =
                    hideoutTable.Production.Recipes != null
                    && hideoutTable.Production.Recipes.Any(r => r.Id == recipe.Id);

                if (recipeExists)
                {
                    if (logger.IsLogEnabled(SPTarkov.Common.Models.Logging.LogLevel.Debug))
                        LogHelper.Debug(logger, $"Recipe {recipe.Id} already exists, skipping");
                    continue;
                }

                hideoutTable.Production.Recipes?.Add(recipe);

                LogHelper.Debug(logger, recipe.ToString());

                if (recipe.EndProductItems != null && recipe.EndProductItems.Count > 0)
                {
                    _extendedRecipes.Add(recipe.Id, recipe);
                }

                LogHelper.Debug(
                    logger,
                    $"Added hideout recipe {recipe.Id} for item {recipe.EndProduct}"
                );
            }

            LogHelper.Debug(
                logger,
                $"Successfully registered {allRecipes.Count} hideout recipes ({_extendedRecipes.Count} extended)"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading hideout recipes: {ex.Message}");
        }
    }

    public Dictionary<MongoId, HideoutProductionExtended> GetExtendedRecipes()
    {
        return _extendedRecipes;
    }

    public HideoutProductionExtended? GetExtendedRecipe(MongoId recipeId)
    {
        _extendedRecipes.TryGetValue(recipeId, out var recipe);
        return recipe ?? null;
    }
}
