using System;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using UnityEngine;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Patches;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib;

[BepInPlugin("com.WTT.ClientCommonLib", "WTT-ClientCommonLib", "1.0.0")]
public class WTTClientCommonLib : BaseUnityPlugin
{
    private static GameWorld _gameWorld;
    public static Player Player;

    private GameObject _updaterObject;
    public AssetLoader AssetLoader;
    public new static ManualLogSource Logger { get; set; }
    public static WTTClientCommonLib Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Logger ??= BepInEx.Logging.Logger.CreateLogSource("DragonDen-TheZoneMaker");
        try
        {
            AssetLoader = new AssetLoader(Logger);
            
            Settings.Init(Config);
            
            new OnGameStarted().Enable();
            new ClothingBundleRendererPatch().Enable();

            var resourceLoader = new ResourceLoader(Logger, AssetLoader);
            resourceLoader.LoadAllResourcesFromServer();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize WTT-ClientCommonLib: {ex}");
        }
    }

    private void Update()
    {
        if (Singleton<GameWorld>.Instantiated && (_gameWorld == null || Player == null))
        {
            _gameWorld = Singleton<GameWorld>.Instance;
            Player = _gameWorld.MainPlayer;
        }
    }
}