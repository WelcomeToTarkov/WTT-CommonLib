using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using WTTClientCommonLib.Services;
using Object = UnityEngine.Object;

namespace WTTClientCommonLib.Patches;

public class ProduceViewShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(ProduceView),
            nameof(ProduceView.Show),
            [
                typeof(ItemUiContext),
                typeof(InventoryController),
                typeof(ProductionScheme),
                typeof(ItemsProducerBase),
                typeof(Action<string>),
                typeof(Action<string>),
                typeof(bool),
            ]
        );
    }

    [PatchPostfix]
    private static void PatchPostfix(
        ProduceView __instance,
        ProductionScheme scheme,
        HideoutItemViewFactory ____resultItemIconViewFactory
    )
    {
        HideoutItemViewFactory viewFactory = ____resultItemIconViewFactory;
        ExtendedRecipeLoader recipeLoader = ExtendedRecipeLoader.Instance;
        string schemeId = scheme._id;
        ExtendedProductionScheme extendedScheme = recipeLoader.GetExtendedScheme(schemeId);

        if (extendedScheme != null)
        {
            int createdStacks = 0;

            List<RecipeResultStack> recipeResultStacks = [];
            foreach (RecipeResultStack resultStack in extendedScheme.ResultItemStacks.Values)
            {
                recipeResultStacks.Add(resultStack);
            }

            // update initial end product
            RecipeResultStack firstStack = extendedScheme.FirstResult;
            viewFactory.Show(
                firstStack.Item,
                __instance.InventoryController,
                __instance.ItemUiContext
            );

            if (
                firstStack.MinStackCount >= 1
                && firstStack.MinStackCount < firstStack.MaxStackCount
            )
            {
                viewFactory.SetCounterText(
                    $"{StackCountDisplayHelper.GetShortBalance(firstStack.MinStackCount.Value, firstStack.Item.TemplateId.ToString())} - {StackCountDisplayHelper.GetShortBalance(firstStack.MaxStackCount.Value, firstStack.Item.TemplateId.ToString())}"
                );
                viewFactory.ShowInfo(true, false);
            }
            else if (firstStack.Count > 1)
            {
                viewFactory.SetCounterText(extendedScheme.FirstResult.Count.ToString());
                viewFactory.ShowInfo(true, false);
            }
            else
            {
                viewFactory.ShowInfo(false, false);
            }

            // count the initial view as a stack
            createdStacks += 1;

            // instantiate previews for additional results
            // spoiler alert: this SUUUUUCKS :waytoodank:
            int totalResultCount = extendedScheme.EndProductItems.Count;
            if (createdStacks != totalResultCount)
            {
                int additionalResults = totalResultCount - createdStacks;

                // fix height
                HorizontalLayoutGroup produceLayout =
                    __instance.gameObject.GetComponent<HorizontalLayoutGroup>();
                produceLayout.childScaleHeight = true;

                // create horizontal list object
                GameObject resultList = new GameObject("CraftResults");
                resultList.transform.parent = __instance.transform;
                resultList.transform.SetSiblingIndex(resultList.transform.GetSiblingIndex() - 1);

                // initialize components and properties for horizontal list
                HorizontalLayoutGroup layoutGroup =
                    resultList.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.spacing = 10f;

                ____resultItemIconViewFactory.transform.parent = resultList.transform;

                // create additional copies
                List<GameObject> additionalViews = [];
                for (int i = 0; i < additionalResults; i++)
                {
                    GameObject factory = GameObject.Instantiate(
                        ____resultItemIconViewFactory.gameObject,
                        resultList.transform,
                        true
                    );
                    additionalViews.Add(factory);
                }

                // set up additional views
                foreach (GameObject factoryGameObject in additionalViews)
                {
                    HideoutItemViewFactory factory =
                        factoryGameObject.GetComponent<HideoutItemViewFactory>();

                    // delete the original view
                    Transform viewContainer = factory.transform.Find("ItemViewContainer");
                    List<Transform> children = viewContainer.GetChildren();
                    foreach (Transform child in children)
                    {
                        if (child.gameObject.name.Contains("hideout_layout"))
                        {
                            Object.Destroy(child.gameObject);
                        }
                    }

                    // create a new view from current result stack
                    RecipeResultStack resultStack = recipeResultStacks[createdStacks];
                    factory.Show(
                        resultStack.Item,
                        __instance.InventoryController,
                        __instance.ItemUiContext
                    );
                    // use result stack count for item count
                    if (
                        resultStack.MinStackCount >= 1
                        && resultStack.MaxStackCount > resultStack.MinStackCount
                    )
                    {
                        viewFactory.SetCounterText(
                            $"{StackCountDisplayHelper.GetShortBalance(resultStack.MinStackCount.Value, resultStack.Item.TemplateId.ToString())} - {StackCountDisplayHelper.GetShortBalance(resultStack.MaxStackCount.Value, resultStack.Item.TemplateId.ToString())}"
                        );
                        factory.ShowInfo(true, false);
                    }
                    else if (resultStack.Count > 1)
                    {
                        factory.SetCounterText(resultStack.Count.ToString());
                        factory.ShowInfo(true, false);
                    }
                    else
                    {
                        factory.ShowInfo(false, false);
                    }

                    createdStacks += 1;
                }
            }
        }
    }
}

public class ProduceViewLoadedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ProduceView), nameof(ProduceView.Show));
    }

    [PatchPostfix]
    private static void PatchPostfix(
        ProduceView __instance,
        HideoutItemViewFactory ____resultItemIconViewFactory
    )
    {
        HideoutItemViewFactory viewFactory = ____resultItemIconViewFactory;
        ExtendedRecipeLoader recipeLoader = ExtendedRecipeLoader.Instance;
        string schemeId = __instance.Scheme._id;
        ExtendedProductionScheme extendedScheme = recipeLoader.GetExtendedScheme(schemeId);

        if (extendedScheme != null)
        {
            // update initial end product
            RecipeResultStack firstStack = extendedScheme.FirstResult;
            viewFactory.Show(
                firstStack.Item,
                __instance.InventoryController,
                __instance.ItemUiContext
            );

            if (
                firstStack.MinStackCount >= 1
                && firstStack.MaxStackCount > firstStack.MinStackCount
            )
            {
                viewFactory.SetCounterText(
                    $"{StackCountDisplayHelper.GetShortBalance(firstStack.MinStackCount.Value, firstStack.Item.TemplateId.ToString())} - {StackCountDisplayHelper.GetShortBalance(firstStack.MaxStackCount.Value, firstStack.Item.TemplateId.ToString())}"
                );
                viewFactory.ShowInfo(true, false);
            }
            else
            {
                viewFactory.SetCounterText(extendedScheme.FirstResult.Count.ToString());
                viewFactory.ShowInfo(extendedScheme.FirstResult.Count > 1, false);
            }
        }
    }
}
