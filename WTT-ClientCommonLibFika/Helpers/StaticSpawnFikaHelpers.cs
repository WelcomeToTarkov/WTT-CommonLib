using System;
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using UnityEngine;
using WTTClientCommonLib.Fika.Packets;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Fika.Helpers;

public class StaticSpawnFikaHelpers
{
    public void SubscribeToFikaEvents()
    {
        try
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[WTT-ClientCommonLibFika] Failed to subscribe to Fika events: {ex}");
        }
    }

    private void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
    {
        try
        {
            fikaEvent.Manager.RegisterPacket<StaticSpawnPacket>(OnStaticSpawnPacketReceived);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[WTT-ClientCommonLibFika] Failed to register StaticSpawnPacket: {ex}");
        }
    }

    private void OnStaticSpawnPacketReceived(StaticSpawnPacket packet)
    {
        try
        {
            var prefab = WTTClientCommonLib.Instance.AssetLoader.LoadPrefabFromBundle(packet.BundleName, packet.PrefabName);
            if (prefab == null)
            {
                LogHelper.LogError($"[WTT-ClientCommonLibFika] Could not load prefab {packet.PrefabName} from {packet.BundleName}");
                return;
            }
        
            var rotation = Quaternion.Euler(packet.Rotation);
            UnityEngine.Object.Instantiate(prefab, packet.Position, rotation);
            LogHelper.LogDebug($"[WTT-ClientCommonLibFika] Spawned synced object {packet.PrefabName}");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[WTT-ClientCommonLibFika] Failed to spawn from packet: {ex}");
        }
    }
    
    public static void SendFikaSpawnPacket(CustomSpawnConfig config, Quaternion rotation)
    {
        try
        {
            var eulerAngles = rotation.eulerAngles;
            
            var packet = new StaticSpawnPacket
            {
                PrefabName = config.PrefabName,
                BundleName = config.BundleName,
                Position = config.Position,
                Rotation = eulerAngles,
                LocationID = config.LocationID,
            };
        
            var networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager != null)
            {
                networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, broadcast: true);
                LogHelper.LogDebug($"[WTT-ClientCommonLibFika] Sent Fika spawn packet for {config.PrefabName}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[WTT-ClientCommonLibFika] Failed to send Fika spawn packet: {ex}");
        }
    }
}
