using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using WTTServerCommonLib.Models;
using WTTServerCommonLib.Services;

namespace WTTServerCommonLib.Controllers;

[Injectable(InjectionType.Singleton)]
public class WTTHideoutControllerExtended(
    ISptLogger<HideoutController> logger,
    TimeUtil timeUtil,
    DatabaseService databaseService,
    InventoryHelper inventoryHelper,
    ItemHelper itemHelper,
    SaveServer saveServer,
    PresetHelper presetHelper,
    PaymentHelper paymentHelper,
    EventOutputHolder eventOutputHolder,
    HttpResponseUtil httpResponseUtil,
    ProfileHelper profileHelper,
    HideoutHelper hideoutHelper,
    ScavCaseRewardGenerator scavCaseRewardGenerator,
    ServerLocalisationService serverLocalisationService,
    ProfileActivityService profileActivityService,
    FenceService fenceService,
    CircleOfCultistService circleOfCultistService,
    ICloner cloner,
    ConfigServer configServer,
    WTTCustomHideoutRecipeService recipeService) : HideoutController(
    logger,
    timeUtil,
    databaseService,
    inventoryHelper,
    itemHelper,
    saveServer,
    presetHelper,
    paymentHelper,
    eventOutputHolder,
    httpResponseUtil,
    profileHelper,
    hideoutHelper,
    scavCaseRewardGenerator,
    serverLocalisationService,
    profileActivityService,
    fenceService,
    circleOfCultistService,
    cloner,
    configServer)
{
    public void HandleExtendedRecipe(MongoId sessionID, HideoutProduction recipe, PmcData pmcData, HideoutTakeProductionRequestData request, ItemEventRouterResponse output)
    {
        // Find craft/production in player profile
        MongoId? prodId = null;
        foreach (var (productionId, productionInProfile) in pmcData.Hideout.Production)
        {
            // Skip undefined production objects caused by continious crafts
            if (productionInProfile is null)
            {
                continue;
            }

            // Not craft we're looking for
            if (productionInProfile.RecipeId != request.RecipeId)
            {
                continue;
            }

            // Could be Production or ScavCase
            prodId = productionId; // Set to objects key
            break;
        }

        // If we're unable to find the production, send an error to the client
        if (prodId is null)
        {
            logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_production_in_profile_by_recipie_id", request.RecipeId));

            httpResponseUtil.AppendErrorToOutput(
                output,
                serverLocalisationService.GetText("hideout-unable_to_find_production_in_profile_by_recipie_id", request.RecipeId)
            );

            return;
        }

        // Variables for management of skill
        double craftingExpAmount = 0;
        var counterHoursCrafting = GetCustomSptHoursCraftingTaskConditionCounter(pmcData, recipe);
        var totalCraftingHours = counterHoursCrafting.Value;

        // Array of arrays of item + children
        List<List<Item>> itemAndChildrenToSendToPlayer = [];

        // Reward is a list of multiple items, handle differently compared to regular end product
        var extendedRecipe = recipeService.GetExtendedRecipe(recipe.Id);
        var recipeIsExtended = false;
        if (extendedRecipe != null)
        {
            if (extendedRecipe.EndProductItems?.Count > 0)
            {
                itemAndChildrenToSendToPlayer = HandleExtendedReward(extendedRecipe);
                recipeIsExtended = true;
            }
        }
        
        // Reward is weapon/armor preset, handle differently compared to 'normal' items
        var rewardIsPreset = !recipeIsExtended && presetHelper.HasPreset(recipe.EndProduct);
        if (rewardIsPreset)
        {
            itemAndChildrenToSendToPlayer = HandlePresetReward(recipe);
        }

        UnstackRewardIntoValidSize(recipe, itemAndChildrenToSendToPlayer, rewardIsPreset || recipeIsExtended);

        // Recipe has an `isEncoded` requirement for reward(s), Add `RecodableComponent` property
        if (recipe.IsEncoded ?? false)
        {
            foreach (var rewardItems in itemAndChildrenToSendToPlayer)
            {
                rewardItems.FirstOrDefault()?.AddUpd();

                rewardItems.FirstOrDefault().Upd.RecodableComponent = new UpdRecodableComponent { IsEncoded = true };
            }
        }

        // Build an array of the tools that need to be returned to the player
        List<List<Item>> toolsToSendToPlayer = [];
        pmcData.Hideout.Production.TryGetValue(prodId.Value, out var hideoutProduction);
        if (hideoutProduction.SptRequiredTools?.Count > 0)
        {
            foreach (var tool in hideoutProduction.SptRequiredTools)
            {
                toolsToSendToPlayer.Add([tool]);
            }
        }

        // Check if the recipe is the same as the last one - get bonus when crafting same thing multiple times
        var area = pmcData.Hideout.Areas.FirstOrDefault(area => area.Type == recipe.AreaType);
        if (area is not null && request.RecipeId != area.LastRecipe)
        // 5 points per craft upon the end of production for alternating between 2 different crafting recipes in the same module
        {
            craftingExpAmount += HideoutConfig.CraftingExpAmount; // Default is 12.5, scaled (at 0.4 scale => 5 points per alternating craft)
        }

        // Update variable with time spent crafting item(s)
        // 1.5 (3.75 w/ applying default 0.4 scale) points per 8 hours of crafting
        totalCraftingHours += recipe.ProductionTime;
        if (totalCraftingHours / HideoutConfig.HoursForSkillCrafting >= 1)
        {
            // Spent enough time crafting to get a bonus xp multiplier
            var multiplierCrafting = Math.Floor(totalCraftingHours.Value / HideoutConfig.HoursForSkillCrafting);
            craftingExpAmount += (HideoutConfig.CraftingExpForHoursOfCrafting * multiplierCrafting);
            totalCraftingHours -= HideoutConfig.HoursForSkillCrafting * multiplierCrafting;
        }

        // Make sure we can fit both the craft result and tools in the stash
        var totalResultItems = new List<List<Item>>();
        totalResultItems.AddRange(itemAndChildrenToSendToPlayer);
        totalResultItems.AddRange(toolsToSendToPlayer);

        if (!inventoryHelper.CanPlaceItemsInInventory(sessionID, totalResultItems))
        {
            httpResponseUtil.AppendErrorToOutput(
                output,
                serverLocalisationService.GetText("inventory-no_stash_space"),
                BackendErrorCodes.NotEnoughSpace
            );

            return;
        }

        // Add the crafting result to the stash, marked as FiR
        var addItemsRequest = new AddItemsDirectRequest
        {
            ItemsWithModsToAdd = itemAndChildrenToSendToPlayer,
            FoundInRaid = true,
            UseSortingTable = false,
            Callback = null,
        };
        inventoryHelper.AddItemsToStash(sessionID, addItemsRequest, pmcData, output);
        if (output.Warnings?.Count > 0)
        {
            return;
        }

        // Add the tools to the stash, we have to do this individually due to FiR state potentially being different
        foreach (var toolItem in toolsToSendToPlayer)
        {
            // Note: FIR state will be based on the first item's SpawnedInSession property per item group
            var addToolsRequest = new AddItemsDirectRequest
            {
                ItemsWithModsToAdd = [toolItem],
                FoundInRaid = toolItem.FirstOrDefault()?.Upd?.SpawnedInSession ?? false,
                UseSortingTable = false,
                Callback = null,
            };

            inventoryHelper.AddItemsToStash(sessionID, addToolsRequest, pmcData, output);
            if (output.Warnings?.Count > 0)
            {
                return;
            }
        }

        //  - Increment skill point for crafting
        //  - Delete the production in profile Hideout.Production
        // Hideout Management skill
        // ? Use a configuration variable for the value?
        var globals = databaseService.GetGlobals();
        profileHelper.AddSkillPointsToPlayer(
            pmcData,
            SkillTypes.HideoutManagement,
            globals.Configuration.SkillsSettings.HideoutManagement.SkillPointsPerCraft,
            true
        );

        // Add Crafting skill to player profile
        if (craftingExpAmount > 0)
        {
            profileHelper.AddSkillPointsToPlayer(pmcData, SkillTypes.Crafting, craftingExpAmount, true);

            // TODO: verify this is still giving intellect skill points on live
            var intellectAmountToGive = 0.5 * Math.Round((double)(craftingExpAmount / 15));
            if (intellectAmountToGive > 0)
            {
                profileHelper.AddSkillPointsToPlayer(
                    pmcData,
                    SkillTypes.Intellect,
                    intellectAmountToGive,
                    useSkillProgressRateMultiplier: false
                );
            }
        }

        area.LastRecipe = request.RecipeId;

        // Update profiles hours crafting value
        counterHoursCrafting.Value = totalCraftingHours;

        // Continuous crafts have special handling in EventOutputHolder.updateOutputProperties()
        hideoutProduction.SptIsComplete = true;
        hideoutProduction.SptIsContinuous = recipe.Continuous ?? false;

        // Continuous recipes need the craft time refreshed as it gets created once on initial craft and stays the same regardless of what
        // production.json is set to
        if (recipe.Continuous.GetValueOrDefault(false))
        {
            hideoutProduction.ProductionTime = hideoutHelper.GetAdjustedCraftTimeWithSkills(pmcData, recipe.Id, true);
        }

        // Flag normal (not continuous) crafts as complete
        if (!recipe.Continuous ?? false)
        {
            hideoutProduction.InProgress = false;
        }
    }

    protected List<List<Item>> HandleExtendedReward(HideoutProductionExtended recipe)
    {
        List<List<Item>> resultItems = recipe.GetResultItems();
        List<List<Item>> finalItems = [];

        foreach (var itemWithChildren in resultItems)
        {
            List<Item>? cloned = cloner.Clone(itemWithChildren)?.ReplaceIDs().ToList();
            if (cloned != null)
            {
                cloned.RemapRootItemId();
                finalItems.Add(cloned);
            }
        }

        return finalItems;
    }
}
