using HarmonyLib.Tools;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WTTServerCommonLib.Services.ItemServiceHelpers
{
    [Injectable(InjectionType.Singleton)]
    public class ItemRegistrationHelper(
        ISptLogger<ItemRegistrationHelper> logger,
        DatabaseService databaseService,
        ItemBaseClassService itemBaseClassService,
        ModItemCacheService modItemCacheService,
        ItemHelper itemHelper
    )
    {
        public void UpdateBaseItemPropertiesWithOverrides(TemplateItemProperties? overrideProperties, TemplateItem itemClone)
        {
            if (overrideProperties is null || itemClone.Properties is null)
                return;

            var target = itemClone.Properties;
            var targetType = target.GetType();

            foreach (var member in overrideProperties.GetType().GetMembers())
            {
                var value = member.MemberType switch
                {
                    MemberTypes.Property => ((PropertyInfo)member).GetValue(overrideProperties),
                    MemberTypes.Field => ((FieldInfo)member).GetValue(overrideProperties),
                    _ => null
                };

                if (value is null)
                    continue;

                var targetMember = targetType.GetMember(member.Name).FirstOrDefault();
                if (targetMember is null)
                    continue;

                switch (targetMember.MemberType)
                {
                    case MemberTypes.Property:
                        var prop = (PropertyInfo)targetMember;
                        if (prop.CanWrite)
                            prop.SetValue(target, value);
                        break;

                    case MemberTypes.Field:
                        var field = (FieldInfo)targetMember;
                        if (!field.IsInitOnly)
                            field.SetValue(target, value);
                        break;
                }
            }
        }

        public void AddToItemsDb(string newItemId, TemplateItem itemToAdd)
        {
            if (!databaseService.GetItems().TryAdd(newItemId, itemToAdd))
                logger.Warning($"Unable to add: {newItemId} To Database");
        }

        public void AddToHandbookDb(string newItemId, string parentId, int priceRoubles)
        {
            databaseService.GetTemplates().Handbook.Items.Add(
                new HandbookItem
                {
                    Id = new MongoId(newItemId),
                    ParentId = parentId,
                    Price = priceRoubles
                });
        }

        public void AddToLocaleDbs(Dictionary<string, LocaleDetails> localeDetails, string newItemId)
        {
            var defaultLocale = localeDetails.Keys.FirstOrDefault();
            if (defaultLocale == null)
                return;

            var languages = databaseService.GetLocales().Languages;
            foreach (var shortNameKey in languages)
            {
                localeDetails.TryGetValue(shortNameKey.Key, out var newLocaleDetails);
                newLocaleDetails ??= localeDetails[defaultLocale];

                if (newLocaleDetails.Name == null)
                    continue;

                if (databaseService.GetLocales().Global.TryGetValue(shortNameKey.Key, out var lazyLoad))
                {
                    lazyLoad.AddTransformer(localeData =>
                    {
                        localeData![$"{newItemId} Name"] = newLocaleDetails.Name;
                        localeData[$"{newItemId} ShortName"] = newLocaleDetails.ShortName ?? "";
                        localeData[$"{newItemId} Description"] = newLocaleDetails.Description ?? "";
                        return localeData;
                    });
                }
            }
        }

        public void AddToFleaPriceDb(string newItemId, int fleaPriceRoubles)
        {
            databaseService.GetTemplates().Prices[newItemId] = fleaPriceRoubles;
        }

        public void AddItemToBaseClassCache(Assembly assembly, string newItemId)
        {
            itemBaseClassService.AddItemToCache(newItemId);
            //modItemCacheService.AddModItem(assembly, newItemId);
        }

        public void AddToWeaponShelf(string newItemId)
        {
            List<MongoId> wallStashIds =
            [
                ItemTpl.HIDEOUTAREACONTAINER_WEAPONSTAND_STASH_1,
                ItemTpl.HIDEOUTAREACONTAINER_WEAPONSTAND_STASH_2,
                ItemTpl.HIDEOUTAREACONTAINER_WEAPONSTAND_STASH_3
            ];

            foreach (var wallId in wallStashIds)
            {
                var wall = itemHelper.GetItem(wallId);
                if (wall.Key)
                    wall.Value.Properties.Grids.First().Properties.Filters.First().Filter.Add(newItemId);
            }
        }
    }
}
