using BepInEx.Logging;
using System;
using System.Collections.Generic;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Services;

public class ExtendedRecipeLoader
{
    public static ExtendedRecipeLoader Instance;

    private Dictionary<string, ExtendedProductionScheme> _extendedSchemes = [];
    
    public ExtendedRecipeLoader()
    {
        if (Instance != null) return;
        
        Instance = this;
    }

    public ExtendedProductionScheme GetExtendedScheme(string schemeId)
    {
        _extendedSchemes.TryGetValue(schemeId, out var extendedProductionScheme);
        return extendedProductionScheme;
    }

    public void FetchExtendedRecipesFromServer()
    {
        try
        {
            var recipeData = Utils.Get<Dictionary<string, ExtendedProductionScheme>>("/wttcommonlib/recipes/extended/get");

            if (recipeData != null)
            {
                _extendedSchemes = recipeData;
            }
            else
            {
                LogHelper.LogError("Failed to fetch extended recipe data from server");
            }
        }
        catch (Exception e)
        {
            LogHelper.LogError(e.ToString());
            throw;
        }
    }

    public void LoadExtendedRecipeResults()
    {
        foreach ((_, ExtendedProductionScheme scheme) in _extendedSchemes)
        {
            scheme.LoadResultItems();
        }
    }
}
