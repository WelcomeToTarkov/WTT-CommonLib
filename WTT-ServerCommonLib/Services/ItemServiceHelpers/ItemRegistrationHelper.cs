using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Items;
using SPTarkov.Server.Core.Services.Modding;

namespace WTTServerCommonLib.Services.ItemServiceHelpers
{
    [Injectable(InjectionType.Singleton)]
    public class ItemRegistrationHelper(
        ISptLogger<ItemRegistrationHelper> logger,
        TemplateTable templateTable,
        LocaleTable localeTable,
        ItemBaseClassService itemBaseClassService,
        ModItemCacheService modItemCacheService,
        ItemHelper itemHelper
    )
    {
        public void UpdateBaseItemPropertiesWithOverrides(
            TemplateItemProperties? overrideProperties,
            TemplateItem itemClone
        )
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
                    _ => null,
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
            if (!templateTable.Items.TryAdd(newItemId, itemToAdd))
                logger.Warning($"Unable to add: {newItemId} To Database");
        }

        public void AddToHandbookDb(string newItemId, string parentId, int priceRoubles)
        {
            templateTable.Handbook.Items.Add(
                new HandbookItem
                {
                    Id = new MongoId(newItemId),
                    ParentId = parentId,
                    Price = priceRoubles,
                }
            );
        }

        public void AddToLocaleDbs(
            Dictionary<string, LocaleDetails> localeDetails,
            string newItemId
        )
        {
            var defaultLocale = localeDetails.Keys.FirstOrDefault();
            if (defaultLocale == null)
                return;

            var languages = localeTable.Languages;
            foreach (var shortNameKey in languages)
            {
                localeDetails.TryGetValue(shortNameKey.Key, out var newLocaleDetails);
                newLocaleDetails ??= localeDetails[defaultLocale];

                if (newLocaleDetails.Name == null)
                    continue;

                if (localeTable.Global.TryGetValue(shortNameKey.Key, out var lazyLoad))
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
            templateTable.Prices[newItemId] = fleaPriceRoubles;
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
                ItemTpl.HIDEOUTAREACONTAINER_WEAPONSTAND_STASH_3,
            ];

            foreach (var wallId in wallStashIds)
            {
                var wall = itemHelper.GetItem(wallId);
                if (wall.Key)
                    wall.Value.Properties.Grids.First()
                        .Properties.Filters.First()
                        .Filter.Add(newItemId);
            }
        }
    }
}
