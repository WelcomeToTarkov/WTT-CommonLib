using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using Diz.Utils;
using EFT;
using EFT.Communications;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using JsonType;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Patches
{
    internal class GetActionsPatch : ModulePatch
    {
        private static ConditionProgressChecker _activeSalvageChecker;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(
                typeof(InteractionContextHelper),
                x =>
                    x.Name == nameof(InteractionContextHelper.GetAvailableActions)
                    && x.GetParameters()[0].Name == "owner"
            );
        }

        [PatchPrefix]
        public static bool PatchPrefix(object[] __args, ref AvailableInteractionState __result)
        {
            var owner = (GamePlayerOwner)__args[0];
            var interactive = __args[1];

            var player = owner.Player;
            if (interactive is TriggerWithId trigger && trigger.TryGetTimeRestrictions(out var restrictions))
            {
                var hour = player.GameWorld.GameDateTime.Calculate().Hour; 
                return Enumerable.Range(restrictions!.from, restrictions.to).Contains(hour);;
            }

            if (interactive is not SalvageItemTrigger salvageTrigger)
                return true;

            var inventoryController = player.InventoryController as InventoryController;
            if (inventoryController == null)
            {
                LogHelper.LogError("InventoryController is null");
                return true;
            }

            var questBook = player.QuestController?.Quests;
            if (questBook == null)
            {
                __result = null;
                return true;
            }

            _activeSalvageChecker = null;
            ConditionProgressChecker activeChecker = null;

            foreach (
                var cpc in questBook.GetConditionHandlersByZone<ConditionZone>(salvageTrigger.Id)
            )
            {
                if (cpc.Condition is not ConditionSalvage)
                    continue;

                if (cpc.CurrentValue < cpc.Condition.value)
                {
                    activeChecker = cpc;
                    break;
                }
            }

            if (activeChecker == null)
            {
                __result = null;
                return false;
            }

            _activeSalvageChecker = activeChecker;

            var actions = new List<InteractionAction>
            {
                new()
                {
                    Name = "SALVAGE_ZONE".Localized(null),
                    Action = () => StartSalvage(owner, salvageTrigger, inventoryController),
                },
            };

            __result = new AvailableInteractionState { Actions = actions };
            return false;
        }

        private static void StartSalvage(
            GamePlayerOwner owner,
            SalvageItemTrigger trigger,
            InventoryController inventoryController
        )
        {
            var player = owner.Player;
            var items = player.Inventory.GetPlayerItems(EPlayerItems.InRaidItems).ToArray();

            MongoID requiredTpl = trigger.RequiredItemTpl;
            var requiredItem = items.FirstOrDefault(i => i.TemplateId == requiredTpl);

            if (requiredItem == null)
            {
                var itemName = requiredTpl.LocalizedName();
                var message = string.Format("YOU_NEED_ITEM_TO_SALVAGE".Localized(null), itemName);

                NotificationManager.DisplaySingletonWarningNotification(message);
                return;
            }

            if (player.CurrentState is not IdlePlayerState)
            {
                NotificationManager.DisplaySingletonWarningNotification(
                    "CANT_SALVAGE_WHILE_MOVING".Localized(null)
                );
                return;
            }

            owner.ShowObjectivesPanel("SALVAGING_OBJECTIVE", trigger.SalvageTime);

            var state = player.CurrentManagedState;
            bool isSilent = true;
            bool isMultitool = false;
            float time = trigger.SalvageTime;
            async void OnPlantComplete(bool success)
            {
                player.PlantItem(requiredItem.TemplateId, trigger.Id, success);
                owner.CloseObjectivesPanel();

                if (!success)
                {
                    owner.ClearInteractionState();
                    return;
                }

                owner.ClearInteractionState();
                await ApplySalvageAsync(trigger, requiredItem, inventoryController, owner);
            }
            state.Plant(isSilent, isMultitool, time, OnPlantComplete);
        }

        private static async Task ApplySalvageAsync(
            SalvageItemTrigger trigger,
            Item requiredItem,
            InventoryController inventoryController,
            GamePlayerOwner owner
        )
        {
            _activeSalvageChecker = null;
            var player = owner.Player;
            var itemUiContext = ItemUiContext.Instance;

            if (itemUiContext == null)
            {
                LogHelper.LogError("ItemUiContext.Instance is null");
                return;
            }

            if (trigger.ConsumeRequiredItem)
            {
                try
                {
                    var removeResult = ItemManipulator.Remove(
                        requiredItem,
                        inventoryController,
                        simulate: true
                    );

                    if (removeResult.Failed)
                    {
                        LogHelper.LogError($"Failed to remove required item: {removeResult.Error}");
                        return;
                    }

                    var netResult = await inventoryController.TryRunNetworkTransaction(
                        removeResult,
                        null
                    );
                    if (!netResult.Succeed)
                    {
                        LogHelper.LogError("Network transaction for removing required item failed");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"Exception removing required item: {ex}");
                    return;
                }
            }

            foreach (var reward in trigger.Rewards)
            {
                for (int i = 0; i < reward.Count; i++)
                {
                    try
                    {
                        var itemFactory = Singleton<ItemFactory>.Instance;
                        if (itemFactory == null)
                        {
                            LogHelper.LogError("ItemFactoryClass.Instance is null");
                            return;
                        }

                        var fakeStash = itemFactory.CreateFakeStash(null);
                        var fakeGrid = new Grid(
                            "salvage fake stash",
                            5,
                            5,
                            false,
                            Array.Empty<ItemFilter>(),
                            fakeStash
                        );

                        fakeStash.Grids[0] = fakeGrid;

                        fakeStash.CurrentAddress = inventoryController.CreateItemAddress();

                        IDatabaseIdGenerator idGen = inventoryController;
                        var newId = new MongoID(idGen.NextId);
                        var rootMongo = new MongoID(fakeStash.Id);

                        var flat = new FlatItem
                        {
                            _id = newId,
                            _tpl = new MongoID(reward.ItemTpl),
                            parentId = rootMongo,
                            slotId = fakeGrid.ID,
                            location = null,
                            upd = null,
                        };

                        var flatArray = new[] { flat };

                        Singleton<ItemFactory>.Instance.FlatItemsToTree(
                            flatArray,
                            false,
                            new Dictionary<string, Item> { { fakeStash.Id, fakeStash } }
                        );

                        foreach (var item in fakeGrid.Items.ToArray())
                        {
                            item.SpawnedInSession = true;

                            IEnumerable<CompoundItem> targets = reward.ToQuestInventory
                                ? new[] { inventoryController.Inventory.QuestRaidItems }.Where(c =>
                                    c != null
                                )
                                : inventoryController
                                    .Inventory.Equipment.ToEnumerable()
                                    .OfType<CompoundItem>();

                            if (!targets.Any())
                            {
                                LogHelper.LogError(
                                    $"No in-raid targets for salvage reward {reward.ItemTpl}"
                                );
                                continue;
                            }

                            const ItemManipulator.EMoveItemOrder flags =
                                ItemManipulator.EMoveItemOrder.PickUp
                                | ItemManipulator.EMoveItemOrder.IgnoreItemParent;

                            var op = ItemManipulator.QuickFindAppropriatePlace(
                                item,
                                inventoryController,
                                targets,
                                flags,
                                simulate: true
                            );

                            if (op.Failed)
                            {
                                LogHelper.LogError(
                                    $"QuickFind failed for salvage reward {reward.ItemTpl}: {op.Error}"
                                );
                                continue;
                            }

                            var tx = await inventoryController.TryRunNetworkTransaction(op, null);
                            if (!tx.Succeed)
                            {
                                LogHelper.LogError(
                                    $"Network transaction failed for salvage reward {reward.ItemTpl}"
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError(
                            $"Error preparing salvage reward {reward.ItemTpl}: {ex}"
                        );
                    }
                }
            }

            try
            {
                player.RemoveTriggerZone(trigger);
                SalvageZoneTracker.Clear(player, trigger);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Error removing salvage trigger: {ex}");
            }
            finally
            {
                _activeSalvageChecker = null;
            }
        }
    }
}
