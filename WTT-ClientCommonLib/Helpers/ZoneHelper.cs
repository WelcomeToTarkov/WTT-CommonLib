using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Helpers;

internal static class ZoneHelper
{
    private static readonly Dictionary<string, (AssetBundle Bundle, int Ref)> _bundles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _owned = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("WTT-ClientCommonLib");
    internal static string BaseDir => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    internal static string PrefabsDir => Path.Combine(BaseDir, "Prefabs");

    internal static string GetPrefabPath(string bundleFileOrEmpty) => Path.Combine(PrefabsDir, bundleFileOrEmpty ?? string.Empty);
    
    private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

    internal static AssetBundle GetOrLoadBundle(string path)
    {
        path = Path.GetFullPath(path);

        if (_bundles.TryGetValue(path, out var info))
        {
            _bundles[path] = (info.Bundle, info.Ref + 1);
            return info.Bundle;
        }

        var pre = FindLoadedByPath(path);
        if (pre)
        {
            _bundles[path] = (pre, 1);
            return pre;
        }

        var b = AssetBundle.LoadFromFile(path);
        if (!b) return null;

        _bundles[path] = (b, 1);
        _owned.Add(path);
        return b;
    }

    internal static void UnloadBundle(string path)
    {
        path = Path.GetFullPath(path);
        if (!_bundles.TryGetValue(path, out var info)) return;

        if (info.Ref > 1)
        {
            _bundles[path] = (info.Bundle, info.Ref - 1);
            return;
        }

        if (_owned.Contains(path))
        {
            info.Bundle.Unload(false);
            _owned.Remove(path);
        }

        _bundles.Remove(path);
        Log.LogInfo($"[WTT-ClientCommonLib] Released AssetBundle: {path}");
    }

    private static AssetBundle FindLoadedByPath(string path)
    {
        path = Path.GetFullPath(path);
        var n1 = Path.GetFileNameWithoutExtension(path);
        var n2 = Path.GetFileName(path);
        var loaded = AssetBundle.GetAllLoadedAssetBundles().ToArray();

        foreach (var ab in loaded)
            if (string.Equals(ab.name, n1, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ab.name, n2, StringComparison.OrdinalIgnoreCase))
                return ab;

        foreach (var ab in loaded)
            if (ab.name.IndexOf(n1, StringComparison.OrdinalIgnoreCase) >= 0 ||
                ab.name.IndexOf(n2, StringComparison.OrdinalIgnoreCase) >= 0)
                return ab;

        return null;
    }

    internal static GameObject TryLoadFirstPrefab(string bundlePath)
    {
        bundlePath = Path.GetFullPath(bundlePath);

        var bundle = GetOrLoadBundle(bundlePath);
        if (!bundle) return null;

        var gos = bundle.LoadAllAssets<GameObject>();
        if (gos == null || gos.Length == 0) return null;

        return gos[0];
    }

    internal static bool TryGetLookHit(Camera cam, string[] skipPrefixes, out Vector3 point)
    {
        point = default;
        var ray = new Ray(cam.transform.position, cam.transform.forward);
        int mask = LayerMask.GetMask("HighPolyCollider", "LowPolyCollider", "Interactive", "Loot", "Terrain", "DoorLowPolyCollider", "Water");
        var hits = new RaycastHit[256];
        var n = Physics.RaycastNonAlloc(ray, hits, Mathf.Infinity, mask);
        for (int i = 0; i < n; i++)
        {
            var go = hits[i].collider.gameObject;
            if (skipPrefixes.Any(p => go.name.StartsWith(p, StringComparison.Ordinal))) continue;
            point = hits[i].point;
            return true;
        }

        return false;
    }

    internal static void DestroyAllColliders(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.Destroy(c);
    }

    internal static GameObject CreateTransparentCube(string objectName, CustomQuestZone customQuestZone = null)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (string.IsNullOrWhiteSpace(objectName))
            objectName = "Random Cube " + Guid.NewGuid();
        
        cube.name = objectName;
        var col = cube.GetComponent<Collider>();
        if (col) UnityEngine.Object.Destroy(col);
        
        if (customQuestZone != null)
        {
            var flareTrigger = cube.AddComponent<ZoneFlareTrigger>();
            flareTrigger.SetId(customQuestZone.ZoneId);
        }

        var renderer = cube.GetComponent<Renderer>();

        // Thank you Timber for this 
        renderer.material.SetOverrideTag("RenderType", "Transparent");
        renderer.material.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
        renderer.material.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
        renderer.material.SetInt(ZWrite, 0);
        renderer.material.DisableKeyword("_ALPHATEST_ON");
        renderer.material.EnableKeyword("_ALPHABLEND_ON");
        renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        renderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.material.color = Settings.ColorZoneGreen;
        cube.GetComponent<Collider>().enabled = false;
        cube.transform.position = Settings.CurrentZoneCubePosition.Value;
        cube.transform.localScale = Settings.DefaultScale.Value;
        var baseCol = Settings.ColorZoneGreen;
        renderer.material.color = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Clamp01(Settings.ZoneCubeTransparency.Value));
        return cube;
    }

    internal static void SetAlpha(Renderer r, float a)
    {
        var c = r.material.color;
        r.material.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
    }

    internal static void WriteIndented(string filePath, object data)
    {
        using var sw = File.CreateText(filePath);
        var ser = new JsonSerializer { Formatting = Formatting.Indented };
        ser.Serialize(sw, data);
    }
}