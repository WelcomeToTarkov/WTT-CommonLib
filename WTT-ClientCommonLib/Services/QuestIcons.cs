using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Services
{
    public static class QuestIcons
    {
        private static Sprite _salvageSprite;
        private static bool _initialized;
        private static bool _failed;

        public static void Init()
        {
            if (_initialized || _failed)
                return;

            try
            {
                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var iconDir = Path.Combine(pluginDir, "QuestIcons");
                var iconPath = Path.Combine(iconDir, "icon_salvage.png");

                if (!File.Exists(iconPath))
                {
                    LogHelper.LogError($"Salvage quest icon not found at {iconPath}");
                    _failed = true;
                    return;
                }

                var bytes = File.ReadAllBytes(iconPath);
                if (bytes == null || bytes.Length == 0)
                {
                    LogHelper.LogError($"Salvage quest icon file is empty: {iconPath}");
                    _failed = true;
                    return;
                }

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!tex.LoadImage(bytes))
                {
                    LogHelper.LogError ($"Failed to load texture for salvage quest icon: {iconPath}");
                    UnityEngine.Object.Destroy(tex);
                    _failed = true;
                    return;
                }

                _salvageSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                _salvageSprite.texture.ignoreMipmapLimit = true;
                _salvageSprite.texture.mipMapBias = -1f;

                LogHelper.LogDebug($"Loaded salvage quest icon from {iconPath}");
                _initialized = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Error loading salvage quest icon: {ex}");
                _failed = true;
            }
        }

        public static Sprite SalvageSprite
        {
            get
            {
                if (!_initialized && !_failed)
                    Init();
                return _salvageSprite;
            }
        }
    }
}