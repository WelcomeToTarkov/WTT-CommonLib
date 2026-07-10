using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using WTTServerCommonLib.Controllers;
using WTTServerCommonLib.Services;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace WTTServerCommonLib;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.wtt.commonlib";
    public override string Name { get; init; } = "WTT-ServerCommonLib";
    public override string Author { get; init; } = "GrooveypenguinX";
    public override List<string>? Contributors { get; init; }
    public override Version Version { get; init; } =
        new(typeof(ModMetadata).Assembly.GetName().Version?.ToString(3));
    public override Range SptVersion { get; init; } = new("~4.1.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override string License { get; init; } = "WTT";
    public override bool HasPrepatcher { get; init; } = false;
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Preload)]
public class WTTServerCommonLib(
    WTTCustomItemServiceExtended customItemServiceExtended,
    WTTCustomAssortSchemeService customAssortSchemeService,
    WTTCustomLootspawnService customLootspawnService,
    WTTCustomQuestService customQuestService,
    WTTCustomLocaleService customLocaleService,
    WTTCustomHideoutRecipeService hideoutRecipeService,
    WTTCustomBotLoadoutService botLoadoutService,
    WTTCustomClothingService customClothingService,
    WTTCustomHeadService customHeadService,
    WTTCustomVoiceService customVoiceService,
    WTTCustomQuestZoneService customQuestZoneService,
    WTTCustomRigLayoutService customRigLayoutService,
    WTTCustomSlotImageService customSlotImageService,
    WTTCustomStaticSpawnService customStaticSpawnService,
    WTTCustomWeaponPresetService customWeaponPresetService,
    WTTCustomBuffService customBuffService,
    WTTCustomProfileService customProfileService,
    WTTCustomAudioService customAudioService,
    WTTCustomAchievementService customAchievementService,
    WTTCustomCustomizationService customCustomizationService,
    WTTCustomDialogueService customDialogueService,
    WTTCustomItemParentService customItemParentService,
    WTTHideoutControllerExtended hideoutControllerExtended,
    WTTCustomQuestItemService customQuestItemService,
    ISptLogger<WTTServerCommonLib> logger,
    IEnumerable<IRuntimePatch> patches
) : IOnLoad
{
    public WTTCustomItemServiceExtended CustomItemServiceExtended { get; } =
        customItemServiceExtended;
    public WTTCustomAssortSchemeService CustomAssortSchemeService { get; } =
        customAssortSchemeService;
    public WTTCustomLootspawnService CustomLootspawnService { get; } = customLootspawnService;
    public WTTCustomQuestService CustomQuestService { get; } = customQuestService;
    public WTTCustomLocaleService CustomLocaleService { get; } = customLocaleService;
    public WTTCustomHideoutRecipeService CustomHideoutRecipeService { get; } = hideoutRecipeService;
    public WTTCustomBotLoadoutService CustomBotLoadoutService { get; } = botLoadoutService;
    public WTTCustomClothingService CustomClothingService { get; } = customClothingService;
    public WTTCustomHeadService CustomHeadService { get; } = customHeadService;
    public WTTCustomVoiceService CustomVoiceService { get; } = customVoiceService;
    public WTTCustomQuestZoneService CustomQuestZoneService { get; } = customQuestZoneService;
    public WTTCustomRigLayoutService CustomRigLayoutService { get; } = customRigLayoutService;
    public WTTCustomSlotImageService CustomSlotImageService { get; } = customSlotImageService;
    public WTTCustomStaticSpawnService CustomStaticSpawnService { get; } = customStaticSpawnService;
    public WTTCustomWeaponPresetService CustomWeaponPresetService { get; } =
        customWeaponPresetService;
    public WTTCustomBuffService CustomBuffService { get; } = customBuffService;
    public WTTCustomProfileService CustomProfileService { get; } = customProfileService;
    public WTTCustomAudioService CustomAudioService { get; } = customAudioService;
    public WTTCustomAchievementService CustomAchievementService { get; } = customAchievementService;
    public WTTCustomCustomizationService CustomCustomizationService { get; } =
        customCustomizationService;
    public WTTCustomDialogueService CustomDialogueService { get; } = customDialogueService;
    public WTTCustomItemParentService CustomItemParentService { get; } = customItemParentService;
    public WTTHideoutControllerExtended HideoutControllerExtended { get; } =
        hideoutControllerExtended;

    public WTTCustomQuestItemService CustomQuestItemService { get; } = customQuestItemService;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }

        CustomLocaleService.CreateCustomLocales(Assembly.GetExecutingAssembly());
        return Task.CompletedTask;
    }
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad)]
public class WTTServerCommonLibPostSptLoad(WTTCustomItemServiceExtended customItemServiceExtended)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        customItemServiceExtended.ProcessDeferredModSlots();
        customItemServiceExtended.ProcessDeferredSecureFilters();
        customItemServiceExtended.ProcessDeferredCalibers();
        return Task.CompletedTask;
    }
}
