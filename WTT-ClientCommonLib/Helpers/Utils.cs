#nullable enable
using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using Newtonsoft.Json;
using SPT.Common.Http;
using UnityEngine;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Helpers;

internal static class Utils

{
    public static Vector3? GetPlayerPosition()
    {
        if (!Singleton<GameWorld>.Instance.MainPlayer)
        {
            ConsoleScreen.Log("Player is null, or you are not ingame.");
            return null;
        }

        var position = ((IPlayer)Singleton<GameWorld>.Instance.MainPlayer).Position;
        return position;
    }

    public static string? GetLocationId()
    {
        if (!Singleton<GameWorld>.Instance)
        {
            ConsoleScreen.Log("Gameworld is null.");
            return null;
        }

        return Singleton<GameWorld>.Instance.LocationId;
    }

    // Access a route from the server
    public static T? Get<T>(string url)
    {
        try
        {
            var req = RequestHandler.GetJson(url);

            // Defensive null & error check
            if (string.IsNullOrWhiteSpace(req) ||
                req == "null" ||
                (req.TrimStart().StartsWith("{") && (req.Contains("\"err\"") || req.Contains("\"error\""))))
            {
                Console.WriteLine($"Invalid/empty/error response from {url}: {req}");
                return default;
            }

            return JsonConvert.DeserializeObject<T>(req);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching {url}: {ex.Message}");
            return default;
        }
    }

    // Convert zone list from custom type to type used by the loader
    public static List<CustomQuestZone> ConvertZoneFormat(List<CustomZoneContainer> customZoneContainer,
        string locationId)
    {
        var convertedZones = new List<CustomQuestZone>();

        customZoneContainer.ForEach(zone =>
        {
            var zoneObject = zone.GameObject;
            var newCustomQuestZone = new CustomQuestZone
            {
                ZoneId = zoneObject.name,
                ZoneName = zoneObject.name,
                ZoneLocation = locationId,
                ZoneType = zone.ZoneType,
                FlareType = zone.FlareZoneType,
                Position = new ZoneTransform(zoneObject.transform.position.x.ToString(),
                    zoneObject.transform.position.y.ToString(), zoneObject.transform.position.z.ToString()),
                Scale = new ZoneTransform(zoneObject.transform.localScale.x.ToString(),
                    zoneObject.transform.localScale.y.ToString(), zoneObject.transform.localScale.z.ToString()),
                Rotation = new ZoneTransform(zoneObject.transform.rotation.x.ToString(),
                    zoneObject.transform.rotation.y.ToString(), zoneObject.transform.rotation.z.ToString(),
                    zoneObject.transform.rotation.w.ToString())
            };
            convertedZones.Add(newCustomQuestZone);
        });
        return convertedZones;
    }
}