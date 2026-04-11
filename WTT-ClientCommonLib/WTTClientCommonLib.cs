using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using UnityEngine;
using WTTClientCommonLib.CommandProcessor;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using WTTClientCommonLib.Patches;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib;


[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("com.wtt.commonlib", "WTT-ClientCommonLib", "2.0.18")]
public class WTTClientCommonLib : BaseUnityPlugin
{
    private static CommandProcessor.CommandProcessor _commandProcessor;
    private static GameWorld _gameWorld;
    public static Player Player;
    private GameObject _updaterObject;
    public AssetLoader AssetLoader;
    private PlayerWorldStats _playerWorldStats;
    public SpawnCommands SpawnCommands;
    
    private static object _fikaHelper;
    private static Assembly _fikaAssembly;
    private static Type _fikaHelperType;
    private static MethodInfo _sendFikaPacketMethod;
    
    public static bool FikaInstalled { get; private set; }
    public static WTTClientCommonLib Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        LogHelper.SetLogger(Logger);
        
        FikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
        
        if (FikaInstalled)
        {
            LogHelper.LogInfo("Fika detected - loading Fika support");
            LoadFikaModule();
        }
        else
        {
            LogHelper.LogInfo("Fika not detected - single-player mode");
        }

        try
        {
            AssetLoader = new AssetLoader(Logger);
            SpawnCommands = new SpawnCommands(Logger, AssetLoader);
            _playerWorldStats = new PlayerWorldStats(Logger);
            
            UniversalConfigManager.Initialize(Config);
            ZoneConfigManager.Initialize(Config);
            StaticSpawnSystemConfigManager.Initialize(Config);
            
            RadioSettings.Init(Config);
            
            new FaceCardViewInitPatch().Enable();
            new FaceCardViewTogglePatch().Enable();
            new BoomboxAudioPatch().Enable();
            new OnGameStarted().Enable();
            new ClothingBundleRendererPatch().Enable();
            new HideoutCustomizationIconPatch().Enable();
            new HideoutCustomizationTexturesPatch().Enable();
            new HideSecretLockedQuestsPatch().Enable();
            new GetActionsPatch().Enable();
            new ConditionSerializerCtorPatch().Enable();
            new Salvage_AddTriggerZonePatch().Enable();
            new Salvage_RemoveTriggerZonePatch().Enable();
            new Salvage_InteractionsChangedPatch().Enable();
            new Salvage_InvokeConditionsPatch().Enable();
            new FixCustomItemSortingOrderPatch().Enable();
            var resourceLoader = new ResourceLoader(Logger, AssetLoader);
            resourceLoader.LoadAllResourcesFromServer();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize WTT-ClientCommonLib: {ex}");
        }
    }

    private void LoadFikaModule()
    {
        try
        {
            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fikaAssemblyPath = Path.Combine(pluginDir, "WTT-ClientCommonLibFika.dll");
            
            if (!File.Exists(fikaAssemblyPath))
            {
                LogHelper.LogError($"Fika module not found at: {fikaAssemblyPath}");
                FikaInstalled = false;
                return;
            }
            
            _fikaAssembly = Assembly.LoadFrom(fikaAssemblyPath);
            _fikaHelperType = _fikaAssembly.GetType("WTTClientCommonLib.Fika.Helpers.StaticSpawnFikaHelpers");
            
            if (_fikaHelperType != null)
            {
                _fikaHelper = Activator.CreateInstance(_fikaHelperType);
                var subscribeMethod = _fikaHelperType.GetMethod("SubscribeToFikaEvents");
                subscribeMethod?.Invoke(_fikaHelper, null);
                
                _sendFikaPacketMethod = _fikaHelperType.GetMethod("SendFikaSpawnPacket", 
                    BindingFlags.Public | BindingFlags.Static);
                
                LogHelper.LogInfo("Fika module loaded and initialized");
            }
            else
            {
                LogHelper.LogError("Could not find StaticSpawnFikaHelpers type in Fika assembly");
                FikaInstalled = false;
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Failed to load Fika module: {ex}");
            FikaInstalled = false;
        }
    }

    public static void SendFikaPacket(CustomSpawnConfig config, Quaternion rotation)
    {
        if (!FikaInstalled || _sendFikaPacketMethod == null)
            return;

        try
        {
            _sendFikaPacketMethod.Invoke(null, new object[] { config, rotation });
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Failed to send Fika packet: {ex}");
        }
    }

    internal void Start()
    {
        if (_commandProcessor == null)
        {
            _commandProcessor = new CommandProcessor.CommandProcessor(_playerWorldStats, SpawnCommands);
            _commandProcessor.RegisterCommandProcessor();
        }

        if (_updaterObject == null)
        {
            _updaterObject = new GameObject("SpawnSystemUpdater");
            _updaterObject.AddComponent<SpawnSystemUpdater>();
            DontDestroyOnLoad(_updaterObject);
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

