using SPTarkov.Common.Models.Logging;

namespace WTTServerCommonLib.Helpers;

public static class LogHelper
{
    public static void Debug<T>(ISptLogger<T> logger, string message)
    {
#if DEBUG
        logger.Info(message);
#endif
    }

    public static void WriteWarning(string message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }

    public static void WriteError(string message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }
}
