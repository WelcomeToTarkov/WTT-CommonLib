# WTT-CommonLib

A comprehensive modding library for SPT that simplifies adding custom content to Escape from Tarkov. WTT-CommonLib handles both server-side database modifications and client-side resource loading automatically - you just configure your content and call the appropriate services.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Available Server Services](#available-server-services)
  - [CustomItemServiceExtended](#customitemserviceextended)
  - [CustomLocaleService](#customlocaleservice)
  - [CustomQuestService](#customquestservice)
  - [CustomQuestZoneService](#customquestzoneservice)
  - [CustomVoiceService](#customvoiceservice)
  - [CustomHeadService](#customheadservice)
  - [CustomClothingService](#customclothingservice)
  - [CustomBotLoadoutService](#custombotloadoutservice)
  - [CustomLootspawnService](#customlootspawnservice)
  - [CustomAssortSchemeService](#customassortschemeservice)
  - [CustomStaticSpawnService](#customstaticspawnservice)
  - [CustomHideoutRecipeService](#customhideoutrecipeservice)
  - [CustomRigLayoutService](#customriglayoutservice)
  - [CustomSlotImageService](#customslotimageservice)
  - [CustomBuffService](#custombuffservice)
  - [CustomProfileService](#customprofileservice)
  - [CustomWeaponPresetService](#customweaponpresetservice)
  - [CustomAudioService](#customaudioservice)
  - [CustomAchievementService](#customachievementservice)
  - [CustomCustomizationService](#customcustomizationservice)
  - [CustomDialogueService](#customdialogueservice)
  - [CustomItemParentService](#customitemparentservice)
- [Example Mod Structure](#example-mod-structure)
- [Available Client Services](#available-client-services)
  - [Server Side Custom Item Template Registration](#server-side-custom-item-template-registration) 

## Features

**Simplified Item Creation** - Clone and modify items with JSON configs  
**Quest System** - Create custom quests with zone-based objectives  
**Character Customization** - Add heads, voices, and clothing  
**Bot Configuration** - Customize AI loadouts and equipment  
**Loot Management** - Control item spawns and distributions  
**Hideout Integration** - Add crafting recipes  
**Multi-language Support** - Easy localization system  

## Installation

1. Download WTT-CommonLib from GitHub or SPT Forge
2. Open the .7z file
3. Drag the SPT and BepInEx folders into your main SPT directory (the one that contains EscapeFromTarkov.exe)

**FOR MOD AUTHORS**

SERVER
1. Download the latest Nuget Package for WTT-ServerCommonLib through your preferred IDE
2. Inject `WTTServerCommonLib` through the constructor
3. Get your current assembly using `Assembly.GetExecutingAssembly()`
4. Pass it to the services you desire to use

CLIENT
1. Download the latest Nuget Package for WTT-ClientCommonLib through your preferred IDE
2. Add `[BepInDependency("com.wtt.commonlib")]` at the top of your main plugin .cs file.

## Quick Start

Here's a minimal example showing how to use WTT-CommonLib:

```csharp
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;

namespace YourModName;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.yourname.yourmod";
    public override string Name { get; init; } = "Your Mod Name";
    public override string Author { get; init; } = "Your Name";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.1");
    public override string License { get; init; } = "MIT";
    public override bool? IsBundleMod { get; init; } = true;
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~2.0.0") }
    };
    public override string? Url { get; init; }
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class YourMod(
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{
    public async Task OnLoad()
    {

        // Get your current assembly
        var assembly = Assembly.GetExecutingAssembly();


        // Use WTT-CommonLib services
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
        
        await Task.CompletedTask;
    }
}
```

### Key Points:
- Inject `WTTServerCommonLib.WTTServerCommonLib` through the constructor
- Set `TypePriority = OnLoadOrder.PostDBModLoader + 2` to load after the database
- Pass your current assembly to utilize any of the public services available

---

## Available Server Services

### CustomItemServiceExtended

**Purpose**: Creates custom items (weapons, armor, consumables, etc.) and integrates them into traders, loot tables, and bot loadouts.

**Usage**:
```csharp
// Use default path (db/CustomItems/)
await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);

// Or specify custom path
await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly,
    Path.Join("db", "MyCustomItemFolder"));
```

**Configuration**: Place JSON files in `db/CustomItems/` (or your custom path):

<details>
<summary> Example Item Configuration (Click to expand)</summary>

```jsonc
{
  // Unique ID for this custom item
  "THIS MUST BE A UNIQUE MONGOID": {
    // The vanilla Tarkov template ID of the item it's copying properties from (e.g., another belt or container)
    "itemTplToClone": "PUT THE MONGOID FOR THE ITEM YOU ARE CLONING HERE",

    // The parent category ID in the game’s item database, controls where it appears in the item tree
    "parentId": "6815465859b8c6ff13f94026",

    // The parent category in the handbook (for sorting/grouping in the UI for FleaMarket)
    "handbookParentId": "5b5f6f8786f77447ed563642",

    // Properties that will override what the "itemTplToClone" has by default
    "overrideProperties": {
      // Make this item examined (visible) by default in the player's inventory
      "ExaminedByDefault": true,

      // Asset prefab for this item: what 3D model and icon the game should use
      "Prefab": {
        // Path to the AssetBundle file for the item
        "path": "Gear_Belts/belt_fannypack.bundle",
        "rcid": ""
      },

      // Inventory size: 2x2 grid spaces
      "Width": 2,
      "Height": 2,

      // How much it weighs (kg)
      "Weight": 0.46,

      // The grid ("container slots") inside the item where you can put other things
      "Grids": [
        {
          "_id": "belt_fannypackgrid",
          "_name": "main",
          "_parent": "6761b213607f9a6f79017aef",
          "_props": {
            "cellsH": 3,
            "cellsV": 2,
            "filters": [
              {
                "Filter": ["54009119af1c881c07000029"] 
              }
            ]
          }
        }
      ]
    },

    // How the item will appear in different languages ("locales")
    "locales": {
      "en": {
        "name": "Fanny Pack",
        "shortName": "Fanny Pack",
        "description": "A fanny pack that can be worn at the waist."
      }
    },

    // Price it will go for on the flea market (in roubles)
    "fleaPriceRoubles": 10900,

    // Price in the handbook
    "handbookPriceRoubles": 7250,

    // Which inventory slots this item can be added to (example slot: "ArmBand")
    "addtoInventorySlots": ["ArmBand"],

    // Should it go in the hideout poster slots?
    "addtoHideoutPosterSlots": true,

    // Should it be visible on posters/maps?  
    "addPosterToMaps": true,

    // How likely it is to appear as a map poster
    "posterSpawnProbability": 10,

    // Should it be available in statuette display slots in hideout?
    "addtoStatuetteSlots": true,

    // (IF YOU ARE MAKING AMMO)Should it add ammo calibers to all clone locations?
    "addCaliberToAllCloneLocations": true,

    // Should be included in static ammo tables (for spawns)
    "addtoStaticAmmo": true,

    // Chance of spawning as static ammo
    "staticAmmoProbability": 5,

    // Should it be available to bots/loadouts? (NOTE - THIS WILL PUSH TO BOT LOOT TABLES AT THE SAME PROBABILITY AS THE ITEM YOU'RE CLONING IF THAT BOT HAS THAT ITEM IN IT'S LOOT TABLE)
    "addtoBots": true,

    // Should it be pushed to special slots
    "addtoSpecialSlots": true,

    // Should it be pushed to anywhere the cloned item is allowed to go
    "addtoModSlots": true,

    // What specific modSlot(s) to search for the cloned item ID and add yours to
    "modSlot": ["mod_muzzle"],

    // Should it be available as a Hall of Fame item?
    "addtoHallOfFame": true,

    // What hall of fame "slots"/categories this can go in
    "hallOfFameSlots": [
      "bigTrophies", 
      "smallTrophies", 
      "dogTags"
    ],

    // Can it be used as generator fuel in hideout?
    "addtoGeneratorAsFuel": true,

    // What stages of the generator it can be used in
    "generatorFuelSlotStages": [
      "1",
      "2",
      "3"
    ],

    // Should it dynamically create a slot for a VANILLA ITEM THAT HAS A BONE PRESENT BUT NO SLOTS IN IT'S PROPS?
    "addtoEmptyPropSlots": true,

    // Details for ONE empty prop slot it should be added to
    "emptyPropSlot": {
      "itemToAddTo": "628a66b41d5e41750e314f34",
      "modSlot": "mod_muzzle"
    },

    // Should it be added to static loot containers (e.g., scav bodies)
    "addtoStaticLootContainers": true,

    // List of containers and the chance it appears in them
    "staticLootContainers": [
      {
        "containerName": "LOOTCONTAINER_DEAD_SCAV", // ID or short name of container (SEE SPT SERVER ITEMTPL CLASS)
        "probability": 54 // percent chance to spawn (NOT 0/100 chance, chance based on that container's loot dumps. Look at specific locations staticLoot.json)
      }
    ],

    // Should it be added to all secure case filters?
    "addtoSecureFilters": true,

    // Should this item be blacklisted from PMC loot/airdrops/daily quest rewards?
    "addtoItemBlacklist": true,

    // Is this a random loot container? (MUST HAVE RANDOMLOOTCONTAINER "62f109593b54472778797866" AS THIS ITEMS PARENT ID)
    "isRandomLootContainer": true,

    // Amount & rewards to give opening this loot container
    "randomLootContainerRewards":
    {
          "rewardCount": 6,
          "foundInRaid": true,
          "rewardTplPool": {
            "57514643245977207f2c2d09": 100,
            "544fb62a4bdc2dfb738b4568": 100,
            "57513f07245977207e26a311": 100,
            "57513f9324597720a7128161": 100,
            "57513fcc24597720a31c09a6": 100,
            "5e8f3423fd7471236e6e3b64": 100,
            "60b0f93284c20f0feb453da7": 100,
            "5734773724597737fd047c14": 100,
            "59e3577886f774176a362503": 100,
            "57505f6224597709a92585a9": 100,
            "544fb6cc4bdc2d34748b456e": 100,
            "5751496424597720a27126da": 100,
            "60098b1705871270cd5352a1": 100,
            "5d40407c86f774318526545a": 100,
            "5c0fa877d174af02a012e1cf": 100,
            "575062b524597720a31c09a1": 100,
            "575146b724597720a27126d5": 100,
            "62a09f32621468534a797acb": 100,
            "57347d7224597744596b4e72": 100,
            "57347d5f245977448b40fa81": 100,
            "65815f0e647e3d7246384e14": 100,
            "635a758bfefc88a93f021b8a": 100,
            "57347d692459774491567cf1": 100,
            "5bc9c29cd4351e003562b8a3": 100,
            "57347d8724597744596b4e76": 100,
            "5c0530ee86f774697952d952": 10
          }
    },

    // Should it be available in trader offers?
    "addtoTraders": true,

    // Custom barter/trader settings
    "traders": {
      "RAGMAN": {
        // Offer ID (unique ID - use a mongoID generator for this)
        "PUT A UNIQUE MONGOID HERE": {
          // Requirements for buying it at the trader
          "barterSettings": {
            "loyalLevel": 1,
            "unlimitedCount": true,
            "stackObjectsCount": 99
          },
          // What you need to trade for it (roubles for example)
          "barters": [
            {
              "count": 26125,
              "_tpl": "MONEY_ROUBLES" // Currency ID or short name (SEE SPT'S ITEMTPL CLASS)
            }
          ]
        }
      }
    }
  }
}
```
</details>

**Features**:
- Clone existing items and modify properties
- Add to trader inventories with custom barters
- Add to bot loadouts (inherits spawn chances from cloned item)
- Add to loot containers
- Weapon preset support
- Weapon mastery integration
- Hall of Fame integration
- Generator fuel integration
- Hideout poster/statuette integration
- Custom inventory slot placement
- Special slot support
- Caliber-based weapon compatibility
- Mod slot propagation (based on the cloned item's locations)
- Add to slots that are empty by default (e.g., mod_muzzle on Keymount muzzles)

---

### CustomLocaleService

**Purpose**: Handles translations for all your custom content.

**Usage**:
```csharp
await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
// Or specify custom path
await wttCommon.CustomLocaleService.CreateCustomLocales(assembly,
    Path.Join("db", "MyCustomLocalesFolder"));
```

**Configuration**: Create locale files in `db/CustomLocales/`:
- `en.json` - English
- `ru.json` - Russian
- `de.json` - German
- etc.

<details>
<summary> Example Locale File (Click to expand)</summary>

```json
{
  "my_custom_weapon_001 Name": "Custom Assault Rifle",
  "my_custom_weapon_001 ShortName": "CAR",
  "my_custom_weapon_001 Description": "A powerful custom rifle",
  "my_custom_quest_001 name": "Custom Quest Name",
  "my_custom_quest_001 description": "Quest description here"
}
```
</details>

---

### CustomQuestService

**Purpose**: Adds custom quests to the database with support for complex objectives, rewards, time windows, and faction restrictions.

**Usage**:
```csharp
await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
// Or specify custom path
await wttCommon.CustomQuestService.CreateCustomQuests(assembly,
    Path.Join("db", "MyCustomQuestsFolder"));
```

**Default Folder Structure**: 

The service expects quests organized by trader in the following structure:

```
db/CustomQuests/
├── QuestTimeData.json          # Optional: Time-limited quest configuration
├── QuestSideData.json          # Optional: Faction-exclusive quest configuration
├── mechanic/                   # Trader folder (can use trader name or ID)
│   ├── Quests/
│   │   └── quest_definitions.json
│   ├── QuestAssort/            # Optional: Quest-unlocked trader items
│   │   └── assort.json
│   ├── Locales/
│   │   ├── en.json
│   │   └── ru.json
│   └── Images/                 # Quest icons
│       └── quest_icon.png
├── prapor/
│   └── ...
└── skier/
    └── ...
```

**Configuration Files**:

**QuestTimeData.json** (Optional - Time-Limited Quests):
```json
{
  "my_quest_id": {
    "StartMonth": 12,
    "StartDay": 1,
    "EndMonth": 12,
    "EndDay": 31
  }
}
```
Quests in this file will only be available during the specified date range. Useful for seasonal/holiday events.

**QuestSideData.json** (Optional - Faction-Exclusive Quests):
```json
{
  "usecOnlyQuests": [
    "quest_id_1",
    "quest_id_2"
  ],
  "bearOnlyQuests": [
    "quest_id_3",
    "quest_id_4"
  ]
}
```
Quests listed here will only be available to the specified PMC faction.

**Quest Assort** (`QuestAssort/*.json`) - Items unlocked after quest completion:
```json
{
  "success": {
    "assort_item_id_to_unlock": "my_quest_id"
  }
}
```

**Locales** (`Locales/*.json`):
```json
{
  "my_quest_id name": "Custom Quest Name",
  "my_quest_id description": "Quest description text",
  "my_quest_id successMessageText": "Quest completion message",
  "my_quest_id startedMessageText": "Quest started message"
}
```

**Images** (`Images/*`): Quest icons referenced by filename (without extension) in the quest definition.

**Features**:
- Time-limited quests (seasonal/date-based via QuestTimeData.json)
- Faction restrictions (BEAR/USEC only via QuestSideData.json)
- Full locale support with fallback to English
- Quest-unlocked trader assortments
- Custom quest icons

**Trader Names**: You can use either trader names (case-insensitive) or trader IDs for folder names:
- `mechanic`, `prapor`, `therapist`, `skier`, `peacekeeper`, `jaeger`, `ragman`, `fence`
- Or their trader IDs: `54cb50c76803fa8b248b4571`, etc.

**Important Notes**:
- Quest .json files **MUST MATCH BSG QUEST MODELS** exactly. Invalid quest data will throw errors and prevent loading.
- QuestTimeData.json and QuestSideData.json are optional and can be placed in the root `CustomQuests/` folder
- If a quest is outside its time window, it will not be loaded into the database
- Images must be in standard formats (.png, .jpg, etc)
- Locales fall back to English if a translation is missing for a specific language

---

### CustomQuestZoneService

**Purpose**: Manages custom quest zones for Visit, PlaceItem, and other location-based objectives.

**Usage**:
```csharp
await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly);
// Or specify custom path
await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly,
    Path.Join("db", "MyCustomQuestZonesFolder"));
```

**Configuration**: Place zone files in `db/CustomQuestZones/`

<details>
<summary> Example Zone Configuration (Click to expand)</summary>

```json
{
  "ZoneId": "deadbody_1",
  "ZoneName": "deadbody_1",
  "ZoneLocation": "woods",
  "ZoneType": "placeitem",
  "FlareType": "",
  "Position": {
    "X": "91.6219",
    "Y": "16.7",
    "Z": "-845.9562"
  },
  "Rotation": {
    "X": "0",
    "Y": "0",
    "Z": "0",
    "W": "1"
  },
  "Scale": {
    "X": "1.5",
    "Y": "3.25",
    "Z": "1.75"
  }
}
```
</details>

**In-Game Editor**: Press **F12** in-game → Navigate to **WTT-ClientCommonLib** settings → Create and position zones visually

---

### CustomVoiceService

**Purpose**: Adds custom character voices for players and bots.

**Usage**:
```csharp
await wttCommon.CustomVoiceService.CreateCustomVoices(assembly);
// Or specify custom path
await wttCommon.CustomVoiceService.CreateCustomVoices(assembly,
    Path.Join("db", "MyCustomVoicesFolder"));
```

**Configuration**: Create voice config files in `db/CustomVoices/`:

<details>
<summary> Example Voice Configuration (Click to expand)</summary>

```json
{
  "6747aa4495b4845a0f3d9f98": {
    "locales": {
      "en": "Duke"
    },
    "name": "Duke",
    "bundlePath": "voices/Duke/Voices/duke_voice.bundle",
    "addVoiceToPlayer": true
  }
}
```
</details>

**Requirements**: Package voice audio into Unity AssetBundles → Place in `bundles/` folder → Add to `bundles.json`

---

### CustomHeadService

**Purpose**: Adds custom character head models for player customization.

**Usage**:
```csharp
await wttCommon.CustomHeadService.CreateCustomHeads(assembly);
// Or specify custom path
await wttCommon.CustomHeadService.CreateCustomHeads(assembly,
    Path.Join("db", "MyCustomHeadsFolder"));
```

**Configuration**: Create head config files in `db/CustomHeads/`:

<details>
<summary> Example Head Configuration (Click to expand)</summary>

```json
{
  "6747aa715be2c2e443264f32": {
    "path": "heads/chrishead.bundle",
    "addHeadToPlayer": true,
    "side": ["Bear", "Usec"],
    "locales": {
      "en": "Chris Redfield"
    }
  }
}
```
</details>

**Requirements**: Package head models into Unity AssetBundles → Place in `bundles/` folder

---

### CustomClothingService

**Purpose**: Adds custom clothing sets (tops, bottoms) for players.

**Usage**:
```csharp
await wttCommon.CustomClothingService.CreateCustomClothing(assembly);
// Or specify custom path
await wttCommon.CustomClothingService.CreateCustomClothing(assembly,
    Path.Join("db", "MyCustomClothingFolder"));
```

**Configuration**: Create clothing config files in `db/CustomClothing/`:

<details>
<summary> Example Clothing Configuration (Click to expand)</summary>

```json
{
  "type": "top",
  "suiteId": "PUT A UNIQUE MONGOID HERE",
  "outfitId": "PUT A UNIQUE MONGOID HERE",
  "topId": "PUT A UNIQUE MONGOID HERE",
  "handsId": "PUT A UNIQUE MONGOID HERE",
  "locales": {
    "en": {
        "name": "Lara's Tattered Tank Top",
        "description": "Women's Upper"
  },
  "topBundlePath": "clothing/lara_top.bundle",
  "handsBundlePath": "clothing/lara_hands.bundle",
  "traderId": "RAGMAN",
  "loyaltyLevel": 1,
  "profileLevel": 1,
  "standing": 0,
  "currencyId": "ROUBLES",
  "price": 150,
  "achievementRequirements": [],
  "questRequirements": []
}
```
**Note**: The Custom Clothing Service will attempt to automatically add the services tab to traders that do not already have it.
</details>

---

### CustomBotLoadoutService

**Purpose**: Customizes AI bot equipment, weapons, and appearance.

**Usage**:
```csharp
await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);
// Or specify custom path
await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly,
    Path.Join("db", "MyCustomBotLoadoutsFolder"));
```

**Configuration**: Create bot loadout files in `db/CustomBotLoadouts/`:

<details>
<summary> Example Bot Loadout (Click to expand)</summary>

```json
{
  "chances": {
    "equipment": {
      "FirstPrimaryWeapon": 100
    },
    "weaponMods": {
      "mod_stock": 100,
      "mod_magazine": 100,
      "mod_tactical_002": 65
    }
  },
  "inventory": {
    "equipment": {
      "FirstPrimaryWeapon": {
        "679a6a534f3d279c99b135b9": 500
      }
    },
    "mods": {
      "679a6a534f3d279c99b135b9": {
        "mod_stock": ["679a6e58085b54fdd56f5d0d"],
        "mod_magazine": ["679a702c47bb7fa666fe618e"]
      }
    },
    "Ammo": {
      "Caliber545x39": {
        "61962b617c6c7b169525f168": 1
      }
    }
  }
}
```
</details>

---

### CustomLootspawnService

**Purpose**: Controls where and how often your custom items spawn as loot on maps. Supports both random loot spawns and guaranteed forced spawns for quest objectives.

**Usage**:
```csharp
await wttCommon.CustomLootspawnService.CreateCustomLootspawns(assembly);
// Or specify custom path
await wttCommon.CustomLootspawnService.CreateCustomLootspawns(assembly,
    Path.Join("db", "MyCustomLootspawnsFolder"));
```

**Default Folder Structure**:

```
db/CustomLootspawns/
├── CustomSpawnpoints/           # Random loot spawns (probability-based)
│   ├── woods_spawns.json
│   ├── customs_spawns.json
│   └── factory_spawns.json
└── CustomSpawnpointsForced/     # Guaranteed spawns (for quest items)
    ├── woods_forced.json
    └── customs_forced.json
```

**Configuration Files**:

**Random Loot Spawns** (`CustomSpawnpoints/*.json`):

<details>
<summary> Example CustomSpawnpoints config (Click to expand)</summary>
  
```json
{
  "sandbox": [
        {
            "locationId": "(82.8276, 14.3806, 181.004)",
            "probability": 0.30,
            "template": {
                "Id": "ag43_spawn",
                "IsContainer": false,
                "useGravity": false,
                "randomRotation": false,
                "Position": {
                    "x": 82.8276,
                    "y": 14.3806,
                    "z": 181.004
                },
                "Rotation": {
                    "x": 2.8415,
                    "y": 212.2408,
                    "z": 91.2423
                },
                "IsGroupPosition": false,
                "GroupPositions": [],
                "IsAlwaysSpawn": false,
                "Root": "68cf6c56cb996a3530052b52",
                "Items": [
                    {
                        "composedKey": "-69420",
                        "_id": "PUT A UNIQUE MONGOID HERE",
                        "_tpl": "68cf56067ff6ceab0c2fd49e",
                        "upd": {
                            "SpawnedInSession": true,
                            "Repairable": {
                                "MaxDurability": 100,
                                "Durability": 100
                            },
                            "Foldable": {
                                "Folded": true
                            },
                            "FireMode": {
                                "FireMode": "single"
                            }
                        }
                    }
                ]
            },
            "itemDistribution": [
                {
                    "composedKey": {
                        "key": "-69420"
                    },
                    "relativeProbability": 1
                }
            ]
        }
    ]
}
```
</details>

**Forced Loot Spawns** (`CustomSpawnpointsForced/*.json`) - Always spawns when quest is active:

<details>
<summary> Example CustomSpawnpointsForced config (Click to expand)</summary>
  
```json
{
    "interchange": [     
        {
            "locationId": "(31.7642 38.7517 -22.9169)",
            "probability": 0.25,
            "template": {
                "Id": "quest_item_immortal_poster (1) [8d2f6c4e-9b3a-4e1f-a7d5-2c8b0e9f3a6d]",
                "IsContainer": false,
                "useGravity": false,
                "randomRotation": false,
                "Position": {
                    "x": 31.7642,
                    "y": 38.7517,
                    "z": -23.244
                },
                "Rotation": {
                    "x": 0,
                    "y": 0,
                    "z": 0
                },
                "IsGroupPosition": false,
                "GroupPositions": [],
                "IsAlwaysSpawn": true,
                "Root": "68748750c2bc7bbc4797d713",
                "Items": [
                    {
                        "_id": "PUT A UNIQUE MONGOID HERE",
                        "_tpl": "687464af51ed3be7e4f6f525",
                        "upd": {
                            "StackObjectsCount": 1
                        }
                    }
                ]
            },
            "itemDistribution": [
                {
                    "composedKey": {
                        "key": "687464af51ed3be7e4f6f525"
                    },
                    "relativeProbability": 1
                }
            ]
        }
    ]
}
```
</details>

**Map Names**: Use the following map identifiers (case-sensitive):
- `bigmap` - Customs
- `woods` - Woods
- `factory4_day` / `factory4_night` - Factory
- `interchange` - Interchange
- `lighthouse` - Lighthouse
- `rezervbase` - Reserve
- `shoreline` - Shoreline
- `tarkovstreets` - Streets of Tarkov
- `laboratory` - Labs
- `sandbox` - Ground Zero

**Use Cases**:

- **Quest Items**: Use `CustomSpawnpointsForced/` for items players must find for quests
- **Multiple Locations**: Use `GroupPositions` array to define several possible spawn positions for variety

---

### CustomAssortSchemeService

**Purpose**: Adds complex, fully-assembled items (like pre-modded weapons or armor with plates) to trader inventories with custom barter schemes. This service gives you complete control over item configuration and pricing.

**Usage**:
```csharp
await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
// Or specify custom path
await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly,
    Path.Join("db", "MyCustomAssortSchemesFolder"));
```

**When to Use This**:
- **Fully-modded weapons** with specific attachments pre-installed
- **Armor with plates** already inserted
- **Complex items** that require nested child items

**Default Folder Structure**:

```
db/CustomAssortSchemes/
├── peacekeeper_assort.json
├── mechanic_assort.json
└── ragman_assort.json
```

**Configuration Structure**:

Each file defines trader assortments with three main sections:

<details>
<summary>Click to expand full configuration example</summary>

```json
{
  "PEACEKEEPER": {
    "items": [
      {
        "_id": "my_custom_weapon_root(PUT A UNIQUE MONGOID HERE)",
        "_tpl": "5447a9cd4bdc2dbd208b4567",
        "upd": {
          "Repairable": {
            "MaxDurability": 100,
            "Durability": 100
          },
          "FireMode": {
            "FireMode": "fullauto"
          },
          "UnlimitedCount": true,
          "StackObjectsCount": 999999,
          "BuyRestrictionMax": 0
        },
        "parentId": "hideout",
        "slotId": "hideout"
      },
      {
        "_id": "PUT A UNIQUE MONGOID HERE",
        "_tpl": "55d480c04bdc2d1d4e8b456a",
        "slotId": "mod_magazine",
        "parentId": "my_custom_weapon_root"
      },
      {
        "_id": "PUT A UNIQUE MONGOID HERE",
        "_tpl": "5649be884bdc2d79388b4577",
        "slotId": "mod_stock",
        "parentId": "my_custom_weapon_root"
      }
    ],
    "barter_scheme": {
      "my_custom_weapon_root": [
        [
          {
            "count": 50000,
            "_tpl": "5449016a4bdc2d6f028b456f"
          }
        ]
      ]
    },
    "loyal_level_items": {
      "my_custom_weapon_root": 2
    }
  }
}
```

</details>

---

### CustomStaticSpawnService

**Purpose**: Places persistent 3D objects on maps with advanced conditions. Supports complex spawn logic including quest status checks, item requirements, boss detection, and linked quest conditions.

**Usage**:
```csharp
await wttCommon.CustomStaticSpawnService.CreateCustomStaticSpawns(assembly);
// Or specify custom path
await wttCommon.CustomStaticSpawnService.CreateCustomStaticSpawns(assembly,
    Path.Join("db", "MyCustomStaticSpawnsFolder"));
```

**Setup**

**1. Package your prefabs into Unity AssetBundles**

Create `.bundle` files containing your prefabs using Unity's AssetBundle build tools.

**2. Register bundles in `bundles.json`**

Add entries to your mod's `bundles.json` manifest:
```json
[
    {
        "key": "staticspawns/my_objects.bundle",
        "dependencies": [
            "assets/commonassets/physics/physicsmaterials.bundle",
            "shaders",
            "cubemaps"
        ]
    },
    {
        "key": "staticspawns/quest_decorations.bundle",
        "dependencies": [
            "assets/commonassets/physics/physicsmaterials.bundle",
            "shaders",
            "cubemaps"
        ]
    }
]
```

**3. Place bundle files in your mod**

```
YourMod/
└── bundles/
    └── staticspawns/
        ├── my_objects.bundle
        └── quest_decorations.bundle
```

**4. Create spawn configuration files**

Place JSON configs in your mod's `db/CustomStaticSpawns/CustomSpawnConfigs/`:

```
db/CustomStaticSpawns/
└── CustomSpawnConfigs/
    ├── interchange_spawns.json
    ├── woods_spawns.json
    └── customs_spawns.json
```

**5. Register in your mod's OnLoad**

```csharp
public async Task OnLoad()
{
    await wttCommon.CustomStaticSpawnService.CreateCustomStaticSpawns(assembly);
}
```

**Configuration Structure**:

<details>
<summary>Example CustomStaticSpawn config (Click to expand)</summary>

```json
[
  {
    "questId": "my_custom_quest_001",
    "locationID": "interchange",
    "bundleName": "my_objects",
    "prefabName": "QuestMarker_001",
    "position": {
      "x": 123.45,
      "y": 15.67,
      "z": -89.01
    },
    "rotation": {
      "x": 0,
      "y": 45,
      "z": 0
    },
    "requiredQuestStatuses": ["Started"],
    "excludedQuestStatuses": ["AvailableForStart"],
    "questMustExist": true,
    "linkedQuestId": null,
    "linkedRequiredStatuses": [],
    "linkedExcludedStatuses": [],
    "linkedQuestMustExist": null,
    "requiredItemInInventory": null,
    "requiredLevel": 10,
    "requiredFaction": "USEC",
    "requiredBossSpawned": null
  },
  {
    "questId": "my_custom_quest_002",
    "locationID": "woods",
    "bundleName": "quest_decorations",
    "prefabName": "TreeMarker_001",
    "position": {
      "x": 200.5,
      "y": 20.0,
      "z": -150.3
    },
    "rotation": {
      "x": 0,
      "y": 0,
      "z": 0
    },
    "requiredQuestStatuses": ["AvailableForStart", "Started"],
    "excludedQuestStatuses": ["Completed"],
    "questMustExist": false,
    "linkedQuestId": "linked_quest_001",
    "linkedRequiredStatuses": ["Completed"],
    "linkedExcludedStatuses": [],
    "linkedQuestMustExist": true,
    "requiredItemInInventory": "some_item_template_id",
    "requiredLevel": null,
    "requiredFaction": null,
    "requiredBossSpawned": "BosBully"
  }
]
```
</details>

**Condition Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `questId` | string | Quest ID that must exist/not exist based on conditions |
| `locationID` | string | Map ID where the object spawns (lowercase, case-sensitive) |
| `bundleName` | string | Name of the AssetBundle containing the prefab (from `bundles.json` key) |
| `prefabName` | string | Name of the GameObject inside the bundle to spawn |
| `position` | XYZ | World position where object spawns |
| `rotation` | XYZ | Euler angles for object rotation |
| `requiredQuestStatuses` | List[string] | Quest must be in one of these statuses to spawn (e.g., "Started", "Completed") |
| `excludedQuestStatuses` | List[string] | Quest must NOT be in these statuses to spawn |
| `questMustExist` | bool | If true, quest must exist in player profile; if false, must not exist |
| `linkedQuestId` | string | Secondary quest ID for linked quest conditions |
| `linkedRequiredStatuses` | List[string] | Linked quest must be in one of these statuses |
| `linkedExcludedStatuses` | List[string] | Linked quest must NOT be in these statuses |
| `linkedQuestMustExist` | bool | If true, linked quest must exist; if false, must not exist |
| `requiredItemInInventory` | string | Player must have this item template in inventory to spawn object |
| `requiredLevel` | int | Player must be at least this level to see the object |
| `requiredFaction` | string | Player must be this faction: "USEC" or "BEAR" |
| `requiredBossSpawned` | string | Boss with this role must be alive in raid (e.g., "BosBully", "BosKnight") |

**In-Game Object Placement Tool**:

Press **~** in-game to access the debug console:

1. **Spawn Object**: `SpawnObject <bundleName> <prefabName>`
   - Spawns object in front of you
   
2. **Enter Edit Mode**: `EnterEditMode`
   - WASD - Move object
   - Numpad 1-9 - Rotate
   - Shift - 2x speed multiplier
   - Shift+Alt - 4x speed multiplier
   - Enter - Backslash
   - Delete key - Remove selected object
   - Period - Cycle through spawned objects
   - Comma - Cycle through spawned objects

3. **Export Spawns**: `ExportSpawnedObjectInfo`
   - Saves all placed objects to JSON file
   - Outputs to: `WTT-ClientCommonLib-CustomStaticSpawnConfig-Output-{timestamp}.json`

**Example Workflow**:

1. Build Unity prefabs and package as AssetBundles (my_objects.bundle)
2. Register bundle in `bundles.json` with key `staticspawns/my_objects.bundle`
3. Place bundle in `bundles/staticspawns/my_objects.bundle`
4. Run in-game: `SpawnObject my_objects QuestMarker_001`
5. Position object with edit mode controls
6. Run: `ExportSpawnedObjectInfo` to generate config JSON
7. Move JSON to `db/CustomStaticSpawns/CustomSpawnConfigs/`
8. Call `CreateCustomStaticSpawns(assembly)` to load your configurations

**Notes**:

- Bundle names in configs must match either the path to your bundle from your `bundles.json` (i.e `StaticSpawns/my_objects.bundle`) OR the bundle name itself (without `.bundle` extension) 
- All bundles are loaded through SPT's Bundle Manager for consistency and efficiency
- Objects only spawn when all specified conditions are met

---


### CustomHideoutRecipeService

**Purpose**: Creates custom crafting recipes for hideout production modules (Workbench, Lavatory, Medstation, etc.).

**Usage**:
```csharp
await wttCommon.CustomHideoutRecipeService.CreateHideoutRecipes(assembly);
// Or specify custom path
await wttCommon.CustomHideoutRecipeService.CreateHideoutRecipes(assembly,
    Path.Join("db", "MyCustomHideoutRecipesFolder"));
```

**Configuration**: Place recipe JSON files in `db/CustomHideoutRecipes/`

**Example Recipe**:
```json
{
    "_id": "69dfa367da725f22873d04a3",
    "areaType": 10,
    "requirements": [
        {
            "areaType": 10,
            "requiredLevel": 2,
            "type": "Area"
        },
        {
            "templateId": "5c06779c86f77426e00dd782",
            "count": 1,
            "isFunctional": false,
            "isEncoded": false,
            "type": "Item"
        }
    ],
    "productionTime": 3600,
    "needFuelForAllProductionTime": true,
    "locked": false,
    "endProduct": "57f3c6bd24597738e730fa2f",
    "endProductItems": {
        "69dfa332259cb3202c3d04a2": {
            "count": 3,
            "minStackCount": 1,
            "maxStackCount": 3,
            "items": [
                {
                    "_id": "69daf153ba7df4fce9b709e0",
                    "_tpl": "57f3c6bd24597738e730fa2f",
                    "upd": {
                        "Foldable": {

                        }
                    }
                },
                {
                    "_id": "69daf180c9bc8f153eb709e2",
                    "_tpl": "57d152ec245977144076ccdf",
                    "parentId": "69daf153ba7df4fce9b709e0",
                    "slotId": "mod_pistol_grip"
                },
                {
                    "_id": "69daf190c66b921e14b709e3",
                    "_tpl": "57d1519e24597714373db79d",
                    "parentId": "69daf153ba7df4fce9b709e0",
                    "slotId": "mod_magazine"
                },
                {
                    "_id": "69daf2460704f20205b709e4",
                    "_tpl": "57f3c7e024597738ea4ba286",
                    "parentId": "69daf153ba7df4fce9b709e0",
                    "slotId": "mod_muzzle"
                },
                {
                    "_id": "69daf26e27ca3b1314b709e5",
                    "_tpl": "57f3c8cc2459773ec4480328",
                    "parentId": "69daf2460704f20205b709e4",
                    "slotId": "mod_muzzle"
                }
            ]
        }
    },
    "continuous": false,
    "count": 1,
    "productionLimitCount": 0,
    "isEncoded": false
}
```

**Key Points**:
- Recipe JSON **must match BSG's HideoutProduction model exactly**
- `_id` must be a valid 24-character hex MongoDB ID
- `areaType` determines which hideout module the recipe appears in (10 = Workbench, 2 = Lavatory, 7 = Medstation, etc.)
- `productionTime` is in seconds
- Invalid recipe structure will throw errors and prevent loading

**Optional fields (`HideoutProductionExtended`)**:
- `endProductItems: []`
  - Specifies an array of `CustomCraftResult` to return from the recipe
    - `count` determines the amount of items given
    - `minStackCount` defines the minimum amount of items per stack
    - `maxStackCount` defines the maximum amount of items per stack
    - `items` array of items to return
    - You can specify multiple recipe results
  - If present:
    - `endProduct` is used solely for the crafting menu search. Has to be a valid item template id
    - `minStackCount` and `maxStackCount` are optional values that reward a random amount of a stackable item
    - `count` is ignored, craft amounts come from `CustomCraftResult`

---

### CustomRigLayoutService

**Purpose**: Sends custom rig layouts to the client so it can register them in-game for your items to use.

**Usage**:
```csharp
wttCommon.CustomRigLayoutService.CreateRigLayouts(assembly);
// Or specify custom path
wttCommon.CustomRigLayoutService.CreateRigLayouts(assembly,
    Path.Join("db", "MyCustomRigLayoutsFolder"));
```

**Requirements**:
- Build your rig layout prefabs into Unity AssetBundles
- Place `.bundle` files in `db/CustomRigLayouts/` inside your mod folder

***

### CustomSlotImageService

**Purpose**: Provides custom inventory slot icons for unique items.

**Usage**:
```csharp
wttCommon.CustomSlotImageService.CreateSlotImages(assembly);
// Or specify custom path
wttCommon.CustomSlotImageService.CreateSlotImages(assembly,
    Path.Join("db", "MyCustomSlotImagesFolder"));
```

**Configuration**:
- Place image files (`.png`, `.jpg`, etc) in `db/CustomSlotImages/` inside your mod folder
- Name each file as the slot ID it replaces (filename without extension)
- The slot ID/key will be used for locale entries if needed

***

### CustomBuffService

**Purpose**: Registers custom stimulator buff configurations to the game database, allowing you to create new buff effects for stimulators and other consumables.

**Usage**:
```csharp
// Use default path (db/CustomBuffs/)
await wttCommon.CustomBuffService.CreateCustomBuffs(assembly);

// Or specify custom path
await wttCommon.CustomBuffService.CreateCustomBuffs(assembly,
    Path.Join("db", "MyCustomBuffsFolder"));
```

<details>
<summary> Example Buff Configuration (Click to expand)</summary>

```json
{
    "Geeked": [
        {
            "BuffType": "StaminaRate",
            "Chance": 1,
            "Delay": 1,
            "Duration": 180,
            "Value": 200,
            "AbsoluteValue": true,
            "SkillName": ""
        },
        {
            "BuffType": "WeightLimit",
            "Chance": 1,
            "Delay": 1,
            "Duration": 180,
            "Value": 50,
            "AbsoluteValue": true,
            "SkillName": ""
        },
        {
            "BuffType": "DamageModifier",
            "Chance": 1,
            "Delay": 1,
            "Duration": 180,
            "Value": -15,
            "AbsoluteValue": true,
            "SkillName": ""
        },
        {
            "BuffType": "HealthRate",
            "Chance": 1,
            "Delay": 1,
            "Duration": 180,
            "Value": 3,
            "AbsoluteValue": true,
            "SkillName": ""
        }
    ]
}
```
</details>


***


### CustomProfileService

**Purpose**: Registers custom player profile editions to the game database, allowing you to create alternative starting profiles with custom inventory, skills, and appearance configurations for different PMC factions.

**Usage**:
```csharp
// Use default path (db/CustomProfiles/)
await wttCommon.CustomProfileService.CreateCustomProfiles(assembly);

// Or specify custom path
await wttCommon.CustomProfileService.CreateCustomProfiles(assembly,
    Path.Join("config", "MyCustomProfilesFolder"));
```

**Configuration**: Place profile configuration files in `config/CustomProfiles/`:


**Features**:
- Create faction-specific starting profiles (BEAR/USEC)
- Define custom starting inventory and equipment
- Set initial skill levels and health values
- Configure player stats and progression data

**File Naming**:
- File name (without extension) becomes the profile ID

**Important Notes**:
- Profile structures **must match SPT's ProfileSides model exactly** - invalid data will cause errors

***


### CustomWeaponPresetService

**Purpose**: Registers custom weapon presets to the game database, allowing you to create pre-configured weapon builds with specific attachments that players and bots can select and use.

**Usage**:
```csharp
// Use default path (db/CustomWeaponPresets/)
await wttCommon.CustomWeaponPresetService.CreateCustomWeaponPresets(assembly);

// Or specify custom path
await wttCommon.CustomWeaponPresetService.CreateCustomWeaponPresets(assembly,
    Path.Join("db", "MyCustomWeaponPresetsFolder"));
```

<details>
<summary> Example Weapon Preset Configuration (Click to expand)</summary>

```json
{
    "PUT A UNIQUE MONGOID HERE": {
      "_changeWeaponName": true,
      "_id": "5a32808386f774764a3226d9",
      "_items": [
        {
          "_id": "PUT A UNIQUE MONGOID HERE",
          "_tpl": "5447a9cd4bdc2dbd208b4567"
        },
        {
          "_id": "PUT A UNIQUE MONGOID HERE",
          "_tpl": "59db3a1d86f77429e05b4e92",
          "parentId": "POINTS TO WEAPON ROOT",
          "slotId": "mod_pistol_grip"
        },
        {
          "_id": "PUT A UNIQUE MONGOID HERE",
          "_tpl": "59c1383d86f774290a37e0ca",
          "parentId": "POINTS TO WEAPON ROOT",
          "slotId": "mod_magazine"
        },
      ],
      "_name": "M4A1 2017 New year",
      "_parent": "5a2fa9c4c4a282000d72204f",
      "_type": "Preset"
    }
}
```
</details>

**Features**:
- Create complete weapon builds with all attachments pre-configured
- Multiple presets per configuration file

**Important Notes**:
- Preset structures **must match SPT's Preset model exactly** - invalid preset data will cause errors
- If you are expecting to have a unique named preset, you must also push a locale for that preset for the name to be applied properly

***

## CustomAudioService

### Purpose
Handles registration and management of custom audio for character creation face cards and hideout radios. This service manages audio bundles through SPT's Bundle Manager, eliminating redundant downloads and improving compatibility with multiplayer mods like Fika.

### Usage

Audio bundles are registered through SPT's bundle manifest system rather than being loaded separately, ensuring efficient resource management and compatibility.

### Key Methods

- `RegisterAudioBundles(List<string> audioBundleKeys)`:  
  Registers audio bundle keys that are already defined in your mod's `bundles.json` manifest.

- `CreateFaceCardAudio(string faceName, string audioKey, bool playOnRadioIfFaceIsSelected = false)`:  
  Associates audio clips with a face card, optionally marking them for radio playback when the face is selected.

- `CreateRadioAudio(string audioKey)`:  
  Adds audio clips to the global radio pool, played independently of face selection.

- `GetAudioManifest()`:  
  Returns the complete audio manifest (bundles, face mappings, radio audio).

### Setup

**1. Package your audio into a Unity AssetBundle**

Create a `.bundle` file containing your AudioClip assets using Unity's AssetBundle build tools.

**2. Register the bundle in `bundles.json`**

Add an entry to your mod's `bundles.json` manifest:
```json
{
    "key": "heads/mymodaudio.bundle",
    "dependencyKeys": [
    ]
}
```

**3. Place the bundle file in your mod**

```
YourMod/
└── bundles/
    └── heads/
        └── mymodaudio.bundle
```

**4. Register in your mod's OnLoad**

```csharp
public async Task OnLoad()
{
    // List your audio bundle paths to be sent to the client
    internal static readonly List<string> AudioBundleKeys = new()
    {
        "audio/mymodaudio.bundle"
    };

    // Register your audio bundles
    wttCommon.CustomAudioService.RegisterAudioBundles(AudioBundleKeys)

    // Add face-specific audio
    wttCommon.CustomAudioService.CreateFaceCardAudio("SoldierFace", "soldier_radio_01", true);
    
    // Add standalone radio audio
    wttCommon.CustomAudioService.CreateRadioAudio("ambient_bg_track_01");
}
```

### Configuration Entries

- `FaceCardVolume`:  
  Volume level (0.0 to 1.0) for face card audio during character creation.

- `GymEnabled`:  
  Enable/disable custom audio for the Gym radio.

- `GymReplacementChance`:  
  Percentage (0-100) chance custom audio replaces default Gym radio audio.

- `GymPlayOnFirstEntrance`:  
  Play custom face audio when entering hideout if available.

- `GymRadioVolume`:  
  Volume for Gym radio custom audio (0.0 to 1.0).

- `RestSpaceEnabled`:  
  Enable/disable custom audio for the Rest Space radio.

- `RestSpaceReplacementChance`:  
  Percentage (0-100) chance custom audio replaces default Rest Space audio.

- `RestSpacePlayOnFirstEntrance`:  
  Play custom face audio when entering hideout if available.

- `RestSpaceRadioVolume`:  
  Volume for Rest Space radio custom audio (0.0 to 1.0).

### Notes

- Audio bundles are loaded through SPT's Bundle Manager, eliminating redundant downloads and improving Fika compatibility
- Bundle keys must match entries in your `bundles.json` manifest

***


### CustomAchievementService

**Purpose**: Creates custom achievements, complete with locales and custom images

**Usage**:
```csharp
// Use default path (db/CustomAchievements/)
await wttCommon.CustomAchievementService.CreateCustomAchievements(assembly);

// Or specify custom path
await wttCommon.CustomAchievementService.CreateCustomAchievements(assembly,
    Path.Join("db", "MyCustomAchievementsFolder"));
```

**Default Folder Structure**:

```
db/CustomAchievements/
├── Achievements/
│   ├── achievement_set_001.json
│   └── achievement_set_002.json
├── Locales/
│   ├── en.json
│   ├── ru.json
│   └── de.json
└── Images/
    ├── achievement_icon_001.png
    ├── achievement_icon_002.jpg
    └── achievement_rewards.png
```

**Configuration Files**:

**Achievements** (`Achievements/*.json`) - Achievement definitions:

<details>
<summary>Example Achievement Configuration (Click to expand)</summary>

```json
[
{
    "id": "achievement_001(THIS MUST BE A UNIQUE MONGOID)", 
    "imageUrl": "/files/achievement/hipoint.png",
    "assetPath": "",
    "rewards": [],
    "conditions": {
      "availableForFinish": [
        {
          "id": "THIS MUST BE A UNIQUE MONGOID",
          "index": 0,
          "dynamicLocale": false,
          "visibilityConditions": [],
          "globalQuestCounterId": "",
          "parentId": "",
          "value": 1,
          "type": "Elimination",
          "oneSessionOnly": false,
          "completeInSeconds": 0,
          "doNotResetIfCounterCompleted": false,
          "isResetOnConditionFailed": false,
          "isNecessary": false,
          "counter": {
            "id": "THIS MUST BE A UNIQUE MONGOID",
            "conditions": [
              {
                "id": "THIS MUST BE A UNIQUE MONGOID",
                "dynamicLocale": false,
                "target": "Savage",
                "value": 1,
                "compareMethod": ">=",
                "conditionType": "Kills",
                "weapon": [
                  "679f2453d1970258c1df3fce"
                ],
                "bodyPart": [],
                "daytime": {
                  "from": 0,
                  "to": 0
                },
                "distance": {
                  "compareMethod": ">=",
                  "value": 0
                },
                "resetOnSessionEnd": false,
                "savageRole": [],
                "enemyEquipmentExclusive": [],
                "enemyEquipmentInclusive": [],
                "enemyHealthEffects": [],
                "weaponCaliber": [],
                "weaponModsExclusive": [],
                "weaponModsInclusive": []
              }
            ]
          },
          "conditionType": "CounterCreator"
        }
      ],
      "fail": []
    },
    "instantComplete": false,
    "showNotificationsInGame": true,
    "showProgress": true,
    "prefab": "",
    "rarity": "Common",
    "hidden": false,
    "showConditions": true,
    "progressBarEnabled": true,
    "side": "Pmc",
    "index": 9001
  }
]  
```
</details>

**Locales** (`Locales/*.json`) - Multi-language support:

```json
{
  "achievement_001 name": "Custom Achievement Title",
  "achievement_001 description": "Detailed description of what this achievement requires",
  "achievement_002 name": "Secret Challenge",
  "achievement_002 description": "A hidden achievement description"
}
```

**Important Notes**:
- Achievement JSON files **must match BSG's Achievement model exactly** — invalid achievement data will be skipped with a warning
- Each achievement requires a valid 24-character MongoDB ID
- Images are automatically registered as routes: `/files/achievement/{imageName}`

***

### CustomCustomizationService

**Purpose**: Registers custom hideout decorations, customization items, icons, and shooting range mark textures to the game database.

**Usage**:
```csharp
// Use default paths
await wttCommon.CustomCustomizationService.CreateCustomCustomizations(assembly);

// Or specify custom paths
await wttCommon.CustomCustomizationService.CreateCustomCustomizations(assembly,
    customizationRelativePath: Path.Combine("db", "CustomCustomization", "Customization"),
    storageRelativePath: Path.Combine("db", "CustomCustomization", "CustomizationStorage"),
    hideoutCustomizationRelativePath: Path.Combine("db", "CustomCustomization", "HideoutCustomizationGlobals"),
    hideoutIconsRelativePath: Path.Combine("db", "CustomCustomization", "HideoutIcons"),
    markTexturesRelativePath: Path.Combine("db", "CustomCustomization", "ShootingRangeMarkTextures")
);
```

**Default Configuration**:

Place JSON files and images in your mod's `db/CustomCustomization/` directory:

| Directory | Purpose |
|-----------|---------|
| `Customization/` | Customization item configs (JSON) |
| `CustomizationStorage/` | CustomizationStorage configs (JSON) |
| `HideoutCustomizationGlobals/` | Hideout Customization Globals configs (JSON) |
| `HideoutIcons/` | Icons for Hideout Customizations (`.png`, `.jpg`, etc) |
| `ShootingRangeMarkTextures/` | Custom hi-resolution mark textures for shooting range (`.png`, `.jpg`, etc) |

**Features**:
- Register custom hideout walls, ceilings, floors
- Provides entry for icons
- Add shooting range marks

**Important Notes**:
- All Customization configs **must match BSG's models exactly**. That means CustomizationItems, CustomizationStorage, HideoutCustomizationGlobals all must match SPT's defined models.
***

### CustomDialogueService

**Custom NPC Dialogue Management**

The `WTTCustomDialogueService` loads custom NPC dialogues from JSON/JSONC files and registers them into the game database. This service allows you to extend trader conversations with custom dialogue trees and interactions.

#### Usage

```csharp
// Use default path (db/CustomDialogues)
await wttCommon.CustomDialogueService.CreateCustomDialogues(assembly);

// Or specify custom path
await wttCommon.CustomDialogueService.CreateCustomDialogues(assembly,
    Path.Join("db", "MyCustomDialoguesFolder"));
```

#### Dialogue Format

**IMPORTANT: Your dialogue structure MUST match BSG's dialogue model exactly.**
- View `SPT/SPT_Data/database/templates/dialogue.json` and make sure your dialogue matches one of the "elements" objects 1:1.

***

### CustomItemParentService

Purpose: Registers custom item parent definitions on both the client and server so that items with custom behaviors can be created

#### Example usage

db/CustomParents/example_parent.jsonc
```jsonc
{
    // the id of our custom parent
    "RANDOM_PARENT_MONGOID_HERE": {
        "_id": "RANDOM_PARENT_MONGOID_HERE", // same as the key above
        "_name": "CustomExampleItem", // parent name, should match your client class name
        "_parent": "54009119af1c881c07000029", // base EFT item template this parent derives from
        "_type": "Node",
        
        // any custom props you feel like adding to your parent
        // these can also be defined in your derived items
        "_props": {
            "TestValue": "hi im a test value"
        },
        
        // should the parent be added to item container filters?
        "addToContainerFilters": true,
        
        // list of containers to add the parent to
        "containers": [
            "59fb042886f7746c5005a7b2", // item case
            "59fb023c86f7746d0d4b423c" // weapon case
        ]
    }
}
```

db/CustomItems/item_using_custom_parent.jsonc
```jsonc
"RANDOM_ITEM_MONGOID_HERE": {
    "itemTplToClone": "62a08f4c4f842e1bd12d9d62",
    "parentId": "RANDOM_PARENT_MONGOID_HERE", // has to be same as our custom parent id
    "handbookParentId": "5b47574386f77428ca22b345",
    "overrideProperties": {
        "ExaminedByDefault": true,
        "Prefab": {
            "path": "assets/content/items/barter/item_barter_other_teddyb/item_barter_other_teddyb.bundle",
            "rcid": ""
        },
        "BackgroundColor": "blue",
        "Slots": [],
        "TestValue": "hi im a test value"
    },
    "locales": {
        "en": {
            "name": "Test item with custom parent",
            "shortName": "test_item",
            "description": "test item using our custom parent"
        }
    },
    "clearClonedProps": false,
    "ModdableItemWhitelist": [],
    "ModdableItemBlacklist": [],
    "fleaPriceRoubles": 1,
    "handbookPriceRoubles": 1,
    "addtoTraders": false,
    "traders": {},
    "addtoBots": false,
    "addtostaticlootcontainer": false,
    "StaticLootContainers": [],
    "masteries": false,
    "masterySections": [],
    "addweaponpreset": false,
    "weaponpresets": []
}
```

#### Server
```c#
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class CustomParentModExample(
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    DatabaseService databaseService) : IOnLoad
{
    public async Task OnLoad()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Register your custom parents
        await wttCommon.CustomItemParentService.CreateCustomParents(assembly);
        
        // Register your new custom items using your new item or template types
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        
        await Task.CompletedTask;
    }
}
```

#### Client
```c#
// register our parent to a client-side runtime class
[CustomParent("69ea063bddcf283e70c13532", typeof(CustomExampleItem), typeof(CustomExampleItemTemplate))]
public class CustomExampleItem : Item 
{
    public CustomExampleItem(string id, CustomExampleItemTemplate template) : base(id, template) 
    {
        List<ItemAttributeClass> attributes = [
            new ItemAttributeClass(EItemAttributeId.AmmoCaliber)
            {
                Name = "TestValue",
                StringValue = () => template.TestValue,
                DisplayType = () => EItemAttributeDisplayType.Compact
            }
        ];

        Attributes = attributes;
    }
}

// the base item template doesn't have a json property for TestValue, so it goes into the template's ExtensionData on the server
// ExtensionData values are automatically injected into the template constructor
// client template parameter names are case-insensitive
public class CustomExampleItemTemplate(string testValue) : ItemTemplate 
{
    public readonly string TestValue = testValue;
}

```

***

## Example Mod Structure

### Custom Weapon Mod Structure

```
MyWeaponMod/
├── bundles/
│ └── MyCustomWeapon.bundle
├── db/
│ ├── CustomItems/
│ │ └── weapons.json
│ └── CustomAssortSchemes/
│ 	└── praporAssort.json
├── bundles.json
└── MyWeaponMod.dll
```

***

## Available Client Services

### CustomTemplateIdToObjectService

**Purpose**: Allows other mods to register custom items and template, enabling the game to properly instantiate custom item types

**Usage**:

```csharp
using System.Collections.Generic;
using BepInEx;
using EFT.InventoryLogic;

// Reference to the Client Common Lib - USE THE NUGET PACKAGE!
using WTTClientCommonLib.Services;

namespace YOURMOD
{
    [BepInDependency("com.wtt.commonlib")]
    [BepInPlugin("com.YOURMOD.Core", "YOURMOD", "1.0")]
    internal class YOURMOD : BaseUnityPlugin
    {
        internal void Awake()
        {
            CustomTemplateIdToObjectService.AddNewTemplateIdToObjectMapping(MyCustomItems.CustomMappings);
        }

        public class MyCustomItems
        {
            // TEMPLATE TYPE
            public class MyCustomTemplateType(string myCustomProp1, string myCustomProp2)
                : CompoundItemTemplateClass
            {
                public readonly string MyCustomProp1 = myCustomProp1;
                public readonly string MyCustomProp2 = myCustomProp2;
            }

            // ITEM TYPE
            public class MyCustomItemType(string id, MyCustomTemplateType template) : CompoundItem(id, template)
            {
            }

            // ITEM
            public class MyNewItem : MyCustomItemType
            {
                [GAttribute23] private readonly TagComponent _tag;
                public string MyCustomProp1 { get; }
                public string MyCustomProp2 { get; }

                public MyNewItem(string id, MyCustomTemplateType template) : base(id, template)
                {
                    MyCustomProp1 = template.MyCustomProp1;
                    MyCustomProp2 = template.MyCustomProp2;
                    Components.Add(_tag = new TagComponent(this));
                }

                public override IEnumerable<EItemInfoButton> ItemInteractionButtons
                {
                    get
                    {
                        foreach (var itemInfoButton in GetBaseInteractions())
                        {
                            yield return itemInfoButton;
                        }

                        yield return EItemInfoButton.Install;
                        yield return EItemInfoButton.Uninstall;
                        if (!string.IsNullOrEmpty(_tag?.Name))
                        {
                            yield return EItemInfoButton.ResetTag;
                        }
                    }
                }

                private IEnumerable<EItemInfoButton> GetBaseInteractions()
                {
                    return base.ItemInteractionButtons;
                }
            }

            // Define your items and templates that need to be added to the TemplateIdToObjectMappingClass dictionaries
            // NOTE: Template IDs must match your server-side item definitions in db/templates/items
            public static readonly List<TemplateIdToObjectType> CustomMappings =
            [
                // Register the template type (no item instantiation needed)
                new(
                    "66f16b85ed966fb78f5563d8", // Template ID
                    null, // Item type (null for template-only registration)
                    typeof(MyCustomTemplateType), // Template type
                    null // Constructor (null for template-only registration)
                ),
                // Register the item type with its constructor
                new(
                    "66f17b4cb59dbccbf12990e6", // Template ID
                    typeof(MyNewItem), // Item type
                    typeof(MyCustomTemplateType), // Template type
                    (id, template) => new MyNewItem(id, (MyCustomTemplateType)template) // Constructor
                ),
            ];
        }
    }
}
```

**Mapping Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `TemplateId` | string | Custom item template ID that matches your item template ID in the server database |
| `ItemType` | Type | C# class that inherits from `Item` or one of it's derivative classes (null for template-only registration) |
| `TemplateType` | Type | C# class that represents the template data structure |
| `Constructor` | Delegate | Function that instantiates your custom item: `(id, template) => new YourItem(id, template)` (null for template-only registration) |

**What Gets Registered**:

The service registers your mappings into three internal dictionaries within `TemplateIdToObjectMappingsClass`:

- **TypeTable**: Maps template IDs to their `ItemType` classes for inventory object instantiation
- **TemplateTypeTable**: Maps template IDs to their `TemplateType` classes for data representation
- **ItemConstructors**: Maps template IDs to constructor functions for creating item instances

**Example Use Cases**:

- Creating custom containers or cases with unique inventory layouts (see Pack N Strap)
- Creating custom usable items with accessories and special interactions (see Komrade Kid)

**Important Notes**:

- Both `ItemType` and `TemplateType` must be properly defined C# classes
- Only register custom items — this service will not override vanilla Escape from Tarkov items
- Template IDs in your mappings must match the item template IDs defined on your server database
- You must still add your new templates and items to the server database via `db/templates/items` — this service only handles client-side object instantiation

***

### Server Side Custom Item Template Registration

When using `CustomTemplateIdToObjectService` on the client, you must also register your custom item templates on the server. Here's how to set up custom items in your mod's database initialization:

**Implementation Example**:

```csharp
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using Range = SemanticVersioning.Range;
using Path = System.IO.Path;

namespace YOURMOD.Server;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.YOURNAME.YOURMOD-Server";
    public override string Name { get; init; } = "YOURMOD-Server";
    public override string Author { get; init; } = "YourName";
    public override List<string>? Contributors { get; init; } = null;
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.2");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class YOURMODServer(
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    DatabaseService databaseService) : IOnLoad
{
    public async Task OnLoad()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var itemsDb = databaseService.GetTables().Templates.Items;

        // Base template for your custom item type
        itemsDb["6906a3c931abc0ab8b62d0d2"] = new TemplateItem()
        {
            Id = "6906a3c931abc0ab8b62d0d2",
            Name = "MyCustomItemTemplate",
            Parent = "566162e44bdc2d3f298b4573", // Parent template ID (in this case CompoundItem)
            Type = "Node",
            Properties = new TemplateItemProperties()
        };

        // Your custom item
        itemsDb["6906a400270c1fac09608296"] = new TemplateItem()
        {
            Id = "6906a400270c1fac09608296",
            Name = "MyCustomItemType",
            Parent = "6906a3c931abc0ab8b62d0d2", // Point to your base template
            Type = "Node",
            Properties = new TemplateItemProperties()
        };

        // Register your new custom items using your new item or template types
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        
        await Task.CompletedTask;
    }
}
```

**Key Points**:

- **Template IDs**: Must match the IDs used in your client-side `CustomTemplateIdToObjectService` mappings
- **Parent Template**: Point to an existing item template that matches your item's functionality

***
