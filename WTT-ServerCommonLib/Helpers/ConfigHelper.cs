using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace WTTServerCommonLib.Helpers;

[Injectable]
public class ConfigHelper(ISptLogger<ConfigHelper> logger, JsonUtil jsonUtil)
{
    public T? TryDeserialize<T>(string jsonContent)
        where T : class
    {
        try
        {
            return jsonUtil.Deserialize<T>(jsonContent);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<T>> LoadJsonFileFlexible<T>(string filePath)
        where T : class
    {
        var results = new List<T>();

        if (!File.Exists(filePath))
        {
            logger.Warning($"File not found: {filePath}");
            return results;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var trimmed = json.AsSpan().TrimStart();

            if (trimmed.IsEmpty)
            {
                logger.Warning($"Empty JSON file: {filePath}");
                return results;
            }

            if (trimmed[0] == '[')
            {
                var list = jsonUtil.Deserialize<List<T>>(json);
                if (list != null)
                    results.AddRange(list);
            }
            else if (trimmed[0] == '{')
            {
                var item = jsonUtil.Deserialize<T>(json);
                if (item != null)
                    results.Add(item);
            }
            else
            {
                logger.Warning($"Unsupported JSON root in {filePath}");
            }

            if (results.Count > 0)
                LogHelper.Debug(logger, $"Loaded {results.Count} item(s) from: {filePath}");
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading file {filePath}: {ex.Message}");
        }

        return results;
    }

    public async Task<T?> LoadJsonFile<T>(string filePath)
        where T : class
    {
        if (!File.Exists(filePath))
        {
            logger.Warning($"File not found: {filePath}");
            return null;
        }

        try
        {
            var data = await jsonUtil.DeserializeFromFileAsync<T>(filePath);

            if (data != null)
                LogHelper.Debug(logger, $"Loaded file: {filePath}");
            else
                logger.Warning($"Failed to deserialize {filePath}");

            return data;
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading file {filePath}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<T>> LoadAllJsonFiles<T>(string directoryPath)
    {
        var result = new List<T>();

        if (!Directory.Exists(directoryPath))
            return result;

        var jsonFiles = Directory
            .GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json") || f.EndsWith(".jsonc"))
            .ToArray();

        foreach (var filePath in jsonFiles)
            try
            {
                var jsonData = await jsonUtil.DeserializeFromFileAsync<T>(filePath);
                if (jsonData != null)
                {
                    result.Add(jsonData);
                    LogHelper.Debug(logger, $"Loaded file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading file {filePath}: {ex.Message}");
            }

        return result;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> LoadLocalesFromDirectory(
        string directoryPath
    )
    {
        var locales = new Dictionary<string, Dictionary<string, string>>();

        var jsonFiles = Directory
            .GetFiles(directoryPath, "*.json", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directoryPath, "*.jsonc", SearchOption.AllDirectories))
            .ToArray();

        foreach (var filePath in jsonFiles)
        {
            var localeCode = Path.GetFileNameWithoutExtension(filePath);

            try
            {
                var data = await jsonUtil.DeserializeFromFileAsync<Dictionary<string, string>>(
                    filePath
                );

                if (data != null)
                {
                    locales[localeCode] = data;
                    LogHelper.Debug(logger, $"Loaded locale file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger.Warning($"Failed to parse {filePath}: {ex.Message}");
            }
        }

        return locales;
    }

    public void SaveJsonFileSync<T>(string filePath, T data)
        where T : class
    {
        try
        {
            // Use JsonUtil's synchronous serialize
            var json = jsonUtil.Serialize(data, true);
            File.WriteAllText(filePath, json);
            LogHelper.Debug(logger, $"Saved file: {filePath}");
        }
        catch (Exception ex)
        {
            logger.Error($"Error saving file {filePath}: {ex.Message}");
            throw;
        }
    }
}
