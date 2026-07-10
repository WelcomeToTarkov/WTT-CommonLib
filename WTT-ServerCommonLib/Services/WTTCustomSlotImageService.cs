using System.Reflection;
using SPTarkov.Common.Logger;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomSlotImageService(
    ModHelper modHelper,
    SptLogger<WTTCustomSlotImageService> logger
)
{
    private readonly Dictionary<string, string> _imagePaths = new();

    /// <summary>
    /// Loads custom slot item images from a directory and registers them for client access.
    ///
    /// Images are loaded from the mod's "db/CustomSlotImages" directory (or a custom path if specified).
    ///
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public void CreateSlotImages(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomSlotImages");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
        {
            LogHelper.Debug(logger, $"No CustomSlotImages directory at {finalDir}");
            return;
        }

        string[] extensions = [".png", ".jpg", ".jpeg", ".bmp"];
        foreach (var imagePath in Directory.GetFiles(finalDir))
        {
            var ext = Path.GetExtension(imagePath).ToLowerInvariant();
            if (extensions.Contains(ext))
            {
                var imageName = Path.GetFileNameWithoutExtension(imagePath);
                _imagePaths[imageName] = imagePath;
                LogHelper.Debug(logger, $"Registered slot image: {imageName}");
            }
        }
    }

    public List<string> GetImageManifest()
    {
        return _imagePaths.Keys.ToList();
    }

    public async Task<byte[]?> GetImageData(string imageName)
    {
        if (_imagePaths.TryGetValue(imageName, out var path) && File.Exists(path))
            return await File.ReadAllBytesAsync(path);
        return null;
    }
}
